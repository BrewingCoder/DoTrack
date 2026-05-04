using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DoTrack.Api.Sprints;
using DoTrack.Api.WorkItems;
using DoTrack.Domain.Sprints;
using DoTrack.Integration.Tests.Json;
using Shouldly;

namespace DoTrack.Integration.Tests.Sprints;

[Collection(nameof(IntegrationCollection))]
public sealed class SprintEndpointsTests : IAsyncLifetime
{
    private readonly DoTrackApiFactory _factory;
    private readonly HttpClient _client;

    public SprintEndpointsTests(DoTrackApiFactory factory)
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

    [Fact]
    public async Task Create_Sprint_Returns201_AndShowsInList()
    {
        var (workspace, project, _) = await _factory.SeedAsync();
        var url = $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/sprints";

        var post = await _client.PostAsJsonAsync(url, new
        {
            name = "Sprint 1",
            startsOn = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            endsOn = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(14))
        }, ApiJsonOptions.Default);
        post.StatusCode.ShouldBe(HttpStatusCode.Created);

        var list = await _client.GetFromJsonAsync<SprintResponse[]>(url, ApiJsonOptions.Default);
        list!.Single().Name.ShouldBe("Sprint 1");
        list[0].State.ShouldBe(SprintState.Planning);
    }

    [Fact]
    public async Task Create_EndsBeforeStarts_Returns400()
    {
        var (workspace, project, _) = await _factory.SeedAsync();
        var url = $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/sprints";

        var resp = await _client.PostAsJsonAsync(url, new
        {
            name = "Bad Sprint",
            startsOn = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(7)),
            endsOn = DateOnly.FromDateTime(DateTime.UtcNow.Date)
        }, ApiJsonOptions.Default);
        resp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_State_ToActive_Persists()
    {
        var (workspace, project, _) = await _factory.SeedAsync();
        var url = $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/sprints";
        var post = await _client.PostAsJsonAsync(url, new
        {
            name = "Sprint 1",
            startsOn = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            endsOn = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(14))
        }, ApiJsonOptions.Default);
        var addBody = await post.Content.ReadFromJsonAsync<JsonElement>(ApiJsonOptions.Default);
        var id = addBody.GetProperty("id").GetGuid();

        var patch = await _client.PatchAsJsonAsync($"{url}/{id}", new { state = "Active" }, ApiJsonOptions.Default);
        patch.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var list = await _client.GetFromJsonAsync<SprintResponse[]>(url, ApiJsonOptions.Default);
        list!.Single().State.ShouldBe(SprintState.Active);
    }

    [Fact]
    public async Task AssignItem_ToSprint_AndListIncludesIt()
    {
        var (workspace, project, reporter) = await _factory.SeedAsync();
        var sprintsUrl = $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/sprints";
        var post = await _client.PostAsJsonAsync(sprintsUrl, new
        {
            name = "S1",
            startsOn = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            endsOn = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(14))
        }, ApiJsonOptions.Default);
        var sprintId = (await post.Content.ReadFromJsonAsync<JsonElement>(ApiJsonOptions.Default)).GetProperty("id").GetGuid();

        var itemsUrl = $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/work-items";
        var itemPost = await _client.PostAsJsonAsync(itemsUrl, new { tier = "Item", type = "Task", title = "Sample", reporterId = reporter.Id.Value }, ApiJsonOptions.Default);
        var itemBody = await itemPost.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);

        var assign = await _client.PostAsJsonAsync(
            $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/work-items/{itemBody!.Number}/sprint",
            new { sprintId },
            ApiJsonOptions.Default);
        assign.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var sprintItems = await _client.GetFromJsonAsync<WorkItemResponse[]>(
            $"{sprintsUrl}/{sprintId}/work-items",
            ApiJsonOptions.Default);
        sprintItems!.Length.ShouldBe(1);
        sprintItems[0].Id.ShouldBe(itemBody.Id);
    }

    [Fact]
    public async Task AssignItem_NonItemTier_Returns400()
    {
        var (workspace, project, reporter) = await _factory.SeedAsync();
        var sprintsUrl = $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/sprints";
        var post = await _client.PostAsJsonAsync(sprintsUrl, new
        {
            name = "S1",
            startsOn = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            endsOn = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(14))
        }, ApiJsonOptions.Default);
        var sprintId = (await post.Content.ReadFromJsonAsync<JsonElement>(ApiJsonOptions.Default)).GetProperty("id").GetGuid();

        var itemsUrl = $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/work-items";
        var itemPost = await _client.PostAsJsonAsync(itemsUrl, new { tier = "Epic", title = "Big initiative", reporterId = reporter.Id.Value }, ApiJsonOptions.Default);
        var itemBody = await itemPost.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);

        var assign = await _client.PostAsJsonAsync(
            $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/work-items/{itemBody!.Number}/sprint",
            new { sprintId },
            ApiJsonOptions.Default);
        assign.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteSprint_NullsOutWorkItemAssignments()
    {
        var (workspace, project, reporter) = await _factory.SeedAsync();
        var sprintsUrl = $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/sprints";
        var post = await _client.PostAsJsonAsync(sprintsUrl, new
        {
            name = "S1",
            startsOn = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            endsOn = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(14))
        }, ApiJsonOptions.Default);
        var sprintId = (await post.Content.ReadFromJsonAsync<JsonElement>(ApiJsonOptions.Default)).GetProperty("id").GetGuid();

        var itemsUrl = $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/work-items";
        var itemPost = await _client.PostAsJsonAsync(itemsUrl, new { tier = "Item", type = "Task", title = "Sample", reporterId = reporter.Id.Value }, ApiJsonOptions.Default);
        var itemBody = await itemPost.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);

        await _client.PostAsJsonAsync(
            $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/work-items/{itemBody!.Number}/sprint",
            new { sprintId },
            ApiJsonOptions.Default);

        var del = await _client.DeleteAsync($"{sprintsUrl}/{sprintId}");
        del.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Work item still exists, no longer in any sprint
        var get = await _client.GetAsync($"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/work-items/{itemBody.Number}");
        get.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
