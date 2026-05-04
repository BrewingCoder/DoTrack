using System.Net;
using System.Net.Http.Json;
using DoTrack.Api.WorkItems;
using DoTrack.Domain.WorkItems;
using DoTrack.Integration.Tests.Json;
using Shouldly;

namespace DoTrack.Integration.Tests.WorkItems;

[Collection(nameof(IntegrationCollection))]
public sealed class UpdateWorkItemEndpointTests : IAsyncLifetime
{
    private readonly DoTrackApiFactory _factory;
    private readonly HttpClient _client;

    public UpdateWorkItemEndpointTests(DoTrackApiFactory factory)
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

    private async Task<(string wsSlug, string projKey, int number, Guid reporterId)> SeedItemAsync()
    {
        var (ws, project, reporter) = await _factory.SeedAsync();
        var url = $"/api/v1/workspaces/{ws.Slug}/projects/{project.Key}/work-items";
        var post = await _client.PostAsJsonAsync(url, new
        {
            tier = "Item",
            type = "Task",
            title = "Initial",
            description = "Initial desc",
            reporterId = reporter.Id.Value,
            estimatePoints = 5
        }, ApiJsonOptions.Default);
        var body = await post.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);
        return (ws.Slug, project.Key, body!.Number, reporter.Id.Value);
    }

    [Fact]
    public async Task Patch_TitleOnly_Returns200_WithUpdatedTitle()
    {
        var (ws, key, n, _) = await SeedItemAsync();
        var url = $"/api/v1/workspaces/{ws}/projects/{key}/work-items/{n}";

        var resp = await _client.PatchAsJsonAsync(url, new { title = "New Title" }, ApiJsonOptions.Default);

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);
        body!.Title.ShouldBe("New Title");
        body.Description.ShouldBe("Initial desc");
    }

    [Fact]
    public async Task Patch_State_TransitionsAndAudits()
    {
        var (ws, key, n, _) = await SeedItemAsync();
        var url = $"/api/v1/workspaces/{ws}/projects/{key}/work-items/{n}";

        var resp = await _client.PatchAsJsonAsync(url, new { state = "InProgress" }, ApiJsonOptions.Default);

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);
        body!.State.ShouldBe(WorkItemState.InProgress);

        var get = await _client.GetAsync(url);
        var got = await get.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);
        got!.State.ShouldBe(WorkItemState.InProgress);
    }

    [Fact]
    public async Task Patch_AllFieldsAtOnce_AllApplied()
    {
        var (ws, key, n, _) = await SeedItemAsync();
        var url = $"/api/v1/workspaces/{ws}/projects/{key}/work-items/{n}";

        var resp = await _client.PatchAsJsonAsync(url, new
        {
            title = "Bigger Title",
            description = "Bigger Description",
            estimatePoints = 8,
            state = "Accepted"
        }, ApiJsonOptions.Default);

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);
        body!.Title.ShouldBe("Bigger Title");
        body.Description.ShouldBe("Bigger Description");
        body.EstimatePoints.ShouldBe(8);
        body.State.ShouldBe(WorkItemState.Accepted);
    }

    [Fact]
    public async Task Patch_BlankTitle_Returns400()
    {
        var (ws, key, n, _) = await SeedItemAsync();
        var url = $"/api/v1/workspaces/{ws}/projects/{key}/work-items/{n}";

        var resp = await _client.PatchAsJsonAsync(url, new { title = "  " }, ApiJsonOptions.Default);

        resp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_TitleTooLong_Returns400()
    {
        var (ws, key, n, _) = await SeedItemAsync();
        var url = $"/api/v1/workspaces/{ws}/projects/{key}/work-items/{n}";

        var resp = await _client.PatchAsJsonAsync(url, new { title = new string('a', 513) }, ApiJsonOptions.Default);

        resp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_NegativeEstimate_Returns400()
    {
        var (ws, key, n, _) = await SeedItemAsync();
        var url = $"/api/v1/workspaces/{ws}/projects/{key}/work-items/{n}";

        var resp = await _client.PatchAsJsonAsync(url, new { estimatePoints = -1 }, ApiJsonOptions.Default);

        resp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_AssigneeIdEmpty_Returns400()
    {
        var (ws, key, n, _) = await SeedItemAsync();
        var url = $"/api/v1/workspaces/{ws}/projects/{key}/work-items/{n}";

        var resp = await _client.PatchAsJsonAsync(url, new { assigneeId = Guid.Empty }, ApiJsonOptions.Default);

        resp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_UnknownWorkItem_Returns404()
    {
        var (ws, project, _) = await _factory.SeedAsync();
        var url = $"/api/v1/workspaces/{ws.Slug}/projects/{project.Key}/work-items/999";

        var resp = await _client.PatchAsJsonAsync(url, new { title = "x" }, ApiJsonOptions.Default);

        resp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_UnknownProject_Returns404()
    {
        var (ws, _, _) = await _factory.SeedAsync();
        var url = $"/api/v1/workspaces/{ws.Slug}/projects/NOPE/work-items/1";

        var resp = await _client.PatchAsJsonAsync(url, new { title = "x" }, ApiJsonOptions.Default);

        resp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_OmittedFields_StayUnchanged()
    {
        var (ws, key, n, _) = await SeedItemAsync();
        var url = $"/api/v1/workspaces/{ws}/projects/{key}/work-items/{n}";

        await _client.PatchAsJsonAsync(url, new { title = "Just title" }, ApiJsonOptions.Default);

        var get = await _client.GetAsync(url);
        var body = await get.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);
        body!.Title.ShouldBe("Just title");
        body.Description.ShouldBe("Initial desc");
        body.EstimatePoints.ShouldBe(5);
        body.State.ShouldBe(WorkItemState.Open);
    }
}
