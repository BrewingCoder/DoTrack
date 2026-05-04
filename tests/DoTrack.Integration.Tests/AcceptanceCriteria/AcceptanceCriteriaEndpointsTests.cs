using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DoTrack.Api.AcceptanceCriteria;
using DoTrack.Api.WorkItems;
using DoTrack.Domain.WorkItems;
using DoTrack.Integration.Tests.Json;
using Shouldly;

namespace DoTrack.Integration.Tests.AcceptanceCriteria;

[Collection(nameof(IntegrationCollection))]
public sealed class AcceptanceCriteriaEndpointsTests : IAsyncLifetime
{
    private readonly DoTrackApiFactory _factory;
    private readonly HttpClient _client;

    public AcceptanceCriteriaEndpointsTests(DoTrackApiFactory factory)
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

    private async Task<(string ws, string proj, int n, Guid userId)> SeedAsync()
    {
        var (workspace, project, reporter) = await _factory.SeedAsync();
        var url = $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/work-items";
        var resp = await _client.PostAsJsonAsync(url, new { tier = "Item", type = "Task", title = "Sample", reporterId = reporter.Id.Value }, ApiJsonOptions.Default);
        var body = await resp.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);
        return (workspace.Slug, project.Key, body!.Number, reporter.Id.Value);
    }

    [Fact]
    public async Task Add_Criterion_Returns201_AndShowsInList()
    {
        var (ws, proj, n, _) = await SeedAsync();
        var url = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n}/acceptance-criteria";

        var post = await _client.PostAsJsonAsync(url, new { description = "All tests pass." }, ApiJsonOptions.Default);
        post.StatusCode.ShouldBe(HttpStatusCode.Created);

        var list = await _client.GetFromJsonAsync<AcceptanceCriterionResponse[]>(url, ApiJsonOptions.Default);
        list!.Length.ShouldBe(1);
        list[0].Description.ShouldBe("All tests pass.");
        list[0].Status.ShouldBe(AcceptanceCriterionStatus.Pending);
    }

    [Fact]
    public async Task Add_BlankDescription_Returns400()
    {
        var (ws, proj, n, _) = await SeedAsync();
        var url = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n}/acceptance-criteria";

        var resp = await _client.PostAsJsonAsync(url, new { description = "  " }, ApiJsonOptions.Default);
        resp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_Status_ToMet_Persists()
    {
        var (ws, proj, n, user) = await SeedAsync();
        var url = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n}/acceptance-criteria";

        var post = await _client.PostAsJsonAsync(url, new { description = "Acceptance test" }, ApiJsonOptions.Default);
        var addBody = await post.Content.ReadFromJsonAsync<JsonElement>(ApiJsonOptions.Default);
        var id = addBody.GetProperty("id").GetGuid();

        var patch = await _client.PatchAsJsonAsync($"{url}/{id}", new { status = "Met", userId = user, comment = "Verified in QA" }, ApiJsonOptions.Default);
        patch.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var list = await _client.GetFromJsonAsync<AcceptanceCriterionResponse[]>(url, ApiJsonOptions.Default);
        list!.Single().Status.ShouldBe(AcceptanceCriterionStatus.Met);
        list[0].CheckedByUserId.ShouldBe(user);
        list[0].Comment.ShouldBe("Verified in QA");
    }

    [Fact]
    public async Task Update_Status_ToWaivedWithoutComment_Returns400()
    {
        var (ws, proj, n, user) = await SeedAsync();
        var url = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n}/acceptance-criteria";

        var post = await _client.PostAsJsonAsync(url, new { description = "Acceptance test" }, ApiJsonOptions.Default);
        var addBody = await post.Content.ReadFromJsonAsync<JsonElement>(ApiJsonOptions.Default);
        var id = addBody.GetProperty("id").GetGuid();

        var patch = await _client.PatchAsJsonAsync($"{url}/{id}", new { status = "Waived", userId = user }, ApiJsonOptions.Default);
        patch.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_Status_ToWaivedWithComment_Persists()
    {
        var (ws, proj, n, user) = await SeedAsync();
        var url = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n}/acceptance-criteria";

        var post = await _client.PostAsJsonAsync(url, new { description = "Acceptance test" }, ApiJsonOptions.Default);
        var addBody = await post.Content.ReadFromJsonAsync<JsonElement>(ApiJsonOptions.Default);
        var id = addBody.GetProperty("id").GetGuid();

        var patch = await _client.PatchAsJsonAsync($"{url}/{id}", new { status = "Waived", userId = user, comment = "Client agreed in 7/15 meeting" }, ApiJsonOptions.Default);
        patch.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var list = await _client.GetFromJsonAsync<AcceptanceCriterionResponse[]>(url, ApiJsonOptions.Default);
        list!.Single().Status.ShouldBe(AcceptanceCriterionStatus.Waived);
    }
}
