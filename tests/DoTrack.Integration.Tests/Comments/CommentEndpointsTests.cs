using System.Net;
using System.Net.Http.Json;
using DoTrack.Api.Comments;
using DoTrack.Api.WorkItems;
using DoTrack.Integration.Tests.Json;
using Shouldly;

namespace DoTrack.Integration.Tests.Comments;

[Collection(nameof(IntegrationCollection))]
public sealed class CommentEndpointsTests : IAsyncLifetime
{
    private readonly DoTrackApiFactory _factory;
    private readonly HttpClient _client;

    public CommentEndpointsTests(DoTrackApiFactory factory)
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

    private async Task<(string ws, string proj, int number, Guid authorId)> SeedItemAsync()
    {
        var (workspace, project, reporter) = await _factory.SeedAsync();
        var url = $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/work-items";
        var resp = await _client.PostAsJsonAsync(url, new { tier = "Item", type = "Task", title = "Sample", reporterId = reporter.Id.Value }, ApiJsonOptions.Default);
        var body = await resp.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);
        return (workspace.Slug, project.Key, body!.Number, reporter.Id.Value);
    }

    [Fact]
    public async Task Add_Comment_Returns201()
    {
        var (ws, proj, n, author) = await SeedItemAsync();
        var url = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n}/comments";

        var resp = await _client.PostAsJsonAsync(url, new { authorId = author, body = "Hello", isInternal = false }, ApiJsonOptions.Default);

        resp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var comment = await resp.Content.ReadFromJsonAsync<CommentResponse>(ApiJsonOptions.Default);
        comment!.Body.ShouldBe("Hello");
        comment.IsInternal.ShouldBeFalse();
    }

    [Fact]
    public async Task Add_BlankBody_Returns400()
    {
        var (ws, proj, n, author) = await SeedItemAsync();
        var url = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n}/comments";

        var resp = await _client.PostAsJsonAsync(url, new { authorId = author, body = "  ", isInternal = false }, ApiJsonOptions.Default);

        resp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_FiltersInternalByDefault()
    {
        var (ws, proj, n, author) = await SeedItemAsync();
        var url = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n}/comments";

        await _client.PostAsJsonAsync(url, new { authorId = author, body = "Public", isInternal = false }, ApiJsonOptions.Default);
        await _client.PostAsJsonAsync(url, new { authorId = author, body = "Private", isInternal = true }, ApiJsonOptions.Default);

        var defaultList = await _client.GetFromJsonAsync<CommentResponse[]>(url, ApiJsonOptions.Default);
        defaultList!.Length.ShouldBe(1);
        defaultList[0].Body.ShouldBe("Public");

        var allList = await _client.GetFromJsonAsync<CommentResponse[]>($"{url}?includeInternal=true", ApiJsonOptions.Default);
        allList!.Length.ShouldBe(2);
    }

    [Fact]
    public async Task Add_UnknownWorkItem_Returns404()
    {
        var (workspace, project, _) = await _factory.SeedAsync();
        var url = $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/work-items/999/comments";
        var resp = await _client.PostAsJsonAsync(url, new { authorId = Guid.NewGuid(), body = "x", isInternal = false }, ApiJsonOptions.Default);
        resp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
