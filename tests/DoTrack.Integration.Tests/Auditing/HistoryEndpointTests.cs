using System.Net;
using System.Net.Http.Json;
using DoTrack.Api.Auditing;
using DoTrack.Api.WorkItems;
using DoTrack.Domain.Auditing;
using DoTrack.Integration.Tests.Json;
using Shouldly;

namespace DoTrack.Integration.Tests.Auditing;

[Collection(nameof(IntegrationCollection))]
public sealed class HistoryEndpointTests : IAsyncLifetime
{
    private readonly DoTrackApiFactory _factory;
    private readonly HttpClient _client;

    public HistoryEndpointTests(DoTrackApiFactory factory)
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
    public async Task History_ReturnsInsertAndUpdateRows_InReverseChronologicalOrder()
    {
        var (workspace, project, reporter) = await _factory.SeedAsync();
        var url = $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/work-items";

        var post = await _client.PostAsJsonAsync(url, new { tier = "Item", type = "Task", title = "First", reporterId = reporter.Id.Value }, ApiJsonOptions.Default);
        var posted = await post.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);

        await _client.PatchAsJsonAsync($"{url}/{posted!.Number}", new { title = "Second" }, ApiJsonOptions.Default);
        await _client.PatchAsJsonAsync($"{url}/{posted.Number}", new { state = "InProgress" }, ApiJsonOptions.Default);

        var historyUrl = $"{url}/{posted.Number}/history";
        var rows = await _client.GetFromJsonAsync<AuditLogResponse[]>(historyUrl, ApiJsonOptions.Default);

        rows!.Length.ShouldBe(3);
        rows[0].ChangeType.ShouldBe(ChangeType.Update);
        rows[2].ChangeType.ShouldBe(ChangeType.Insert);
    }

    [Fact]
    public async Task History_FieldChangesPresent_ForUpdate()
    {
        var (workspace, project, reporter) = await _factory.SeedAsync();
        var url = $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/work-items";
        var post = await _client.PostAsJsonAsync(url, new { tier = "Item", type = "Task", title = "Initial", reporterId = reporter.Id.Value }, ApiJsonOptions.Default);
        var posted = await post.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);

        await _client.PatchAsJsonAsync($"{url}/{posted!.Number}", new { title = "Renamed" }, ApiJsonOptions.Default);

        var historyUrl = $"{url}/{posted.Number}/history";
        var rows = await _client.GetFromJsonAsync<AuditLogResponse[]>(historyUrl, ApiJsonOptions.Default);

        var update = rows!.Single(r => r.ChangeType == ChangeType.Update);
        update.FieldChanges.ShouldContain(fc => fc.FieldName == "Title" && fc.NewValue == "Renamed");
    }

    [Fact]
    public async Task History_UnknownWorkItem_Returns404()
    {
        var (workspace, project, _) = await _factory.SeedAsync();
        var resp = await _client.GetAsync($"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/work-items/999/history");
        resp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task History_RespectsLimitParameter()
    {
        var (workspace, project, reporter) = await _factory.SeedAsync();
        var url = $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/work-items";
        var post = await _client.PostAsJsonAsync(url, new { tier = "Item", type = "Task", title = "Initial", reporterId = reporter.Id.Value }, ApiJsonOptions.Default);
        var posted = await post.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);

        for (var i = 0; i < 5; i++)
        {
            await _client.PatchAsJsonAsync($"{url}/{posted!.Number}", new { title = $"v{i}" }, ApiJsonOptions.Default);
        }

        var rows = await _client.GetFromJsonAsync<AuditLogResponse[]>($"{url}/{posted!.Number}/history?limit=2", ApiJsonOptions.Default);
        rows!.Length.ShouldBe(2);
    }
}
