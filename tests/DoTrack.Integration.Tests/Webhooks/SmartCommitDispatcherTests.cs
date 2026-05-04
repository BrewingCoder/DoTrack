using System.Net;
using System.Net.Http.Json;
using DoTrack.Api.Comments;
using DoTrack.Api.WorkItems;
using DoTrack.Domain.WorkItems;
using DoTrack.Integration.Tests.Json;
using Shouldly;

namespace DoTrack.Integration.Tests.Webhooks;

[Collection(nameof(IntegrationCollection))]
public sealed class SmartCommitDispatcherTests : IAsyncLifetime
{
    private readonly DoTrackApiFactory _factory;
    private readonly HttpClient _client;

    public SmartCommitDispatcherTests(DoTrackApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async ValueTask InitializeAsync() => await _factory.ResetDataAsync();

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task<(string ws, string proj, int n, Guid reporterId)> SeedItemAsync()
    {
        var (workspace, project, reporter) = await _factory.SeedAsync();
        var itemsUrl = $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/work-items";
        var resp = await _client.PostAsJsonAsync(itemsUrl,
            new { tier = "Item", type = "Task", title = "Smart-commit target", reporterId = reporter.Id.Value },
            ApiJsonOptions.Default);
        var body = await resp.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);
        return (workspace.Slug, project.Key, body!.Number, reporter.Id.Value);
    }

    private async Task<HttpResponseMessage> PostGitHubPushAsync(string projectKey, int number, string commitMessage)
    {
        var payload = $$"""
        {
          "ref": "refs/heads/main",
          "repository": { "full_name": "BrewingCoder/DoTrack" },
          "commits": [
            {
              "id": "abcdef1234567890abcdef1234567890abcdef12",
              "message": {{System.Text.Json.JsonSerializer.Serialize(commitMessage)}},
              "author": { "name": "Scott", "email": "scott@gscottsingleton.com" },
              "timestamp": "2026-05-04T01:23:45Z",
              "url": "https://github.com/BrewingCoder/DoTrack/commit/abcdef"
            }
          ]
        }
        """;
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/github")
        {
            Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
        };
        req.Headers.Add("X-GitHub-Event", "push");
        return await _client.SendAsync(req);
    }

    [Fact]
    public async Task Push_WithIssueKey_AddsCommitComment()
    {
        var (ws, proj, n, _) = await SeedItemAsync();

        var resp = await PostGitHubPushAsync(proj, n, $"{proj}-{n} normal commit message, no directives");
        resp.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var commentsUrl = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n}/comments";
        var comments = await _client.GetFromJsonAsync<CommentResponse[]>(commentsUrl, ApiJsonOptions.Default);
        comments!.Length.ShouldBe(1);
        comments[0].Body.ShouldContain("Linked from commit");
        comments[0].Body.ShouldContain("BrewingCoder/DoTrack");
    }

    [Fact]
    public async Task Push_WithFixedDirective_TransitionsToAccepted()
    {
        var (ws, proj, n, _) = await SeedItemAsync();

        var resp = await PostGitHubPushAsync(proj, n, $"{proj}-{n} #fixed: closes the bug");
        resp.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var get = await _client.GetAsync($"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n}");
        var body = await get.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);
        body!.State.ShouldBe(WorkItemState.Accepted);
    }

    [Fact]
    public async Task Push_WithInProgressDirective_TransitionsToInProgress()
    {
        var (ws, proj, n, _) = await SeedItemAsync();

        var resp = await PostGitHubPushAsync(proj, n, $"{proj}-{n} #in-progress starting work");
        resp.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var get = await _client.GetAsync($"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n}");
        var body = await get.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);
        body!.State.ShouldBe(WorkItemState.InProgress);
    }

    [Fact]
    public async Task Push_NoMatchingKey_NoOp()
    {
        var (ws, proj, n, _) = await SeedItemAsync();

        var resp = await PostGitHubPushAsync(proj, n, "no issue key here, just a regular message");
        resp.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var commentsUrl = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n}/comments";
        var comments = await _client.GetFromJsonAsync<CommentResponse[]>(commentsUrl, ApiJsonOptions.Default);
        comments!.Length.ShouldBe(0);
    }

    [Fact]
    public async Task Push_UnknownIssueKey_DoesNotFail()
    {
        await SeedItemAsync();

        var resp = await PostGitHubPushAsync("PROJ", 9999, "PROJ-9999 #fixed unknown number");
        resp.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }
}
