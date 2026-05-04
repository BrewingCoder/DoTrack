using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DoTrack.Api.Bootstrap;
using DoTrack.Api.WorkItems;
using DoTrack.Integration.Tests.Json;
using Shouldly;

namespace DoTrack.Integration.Tests.WorkItems;

[Collection(nameof(IntegrationCollection))]
public sealed class WatcherEndpointsTests : IAsyncLifetime
{
    private readonly DoTrackApiFactory _factory;
    private readonly HttpClient _client;

    public WatcherEndpointsTests(DoTrackApiFactory factory)
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

    private async Task<(string ws, string proj, int n, Guid reporterId, Guid otherUserId)> SeedAsync()
    {
        var (workspace, project, reporter) = await _factory.SeedAsync();
        var url = $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/work-items";
        var i = await _client.PostAsJsonAsync(url, new { tier = "Item", type = "Task", title = "x", reporterId = reporter.Id.Value }, ApiJsonOptions.Default);
        var b = await i.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);

        var u = await _client.PostAsJsonAsync("/api/v1/users", new { email = "watcher@e.com", displayName = "W" }, ApiJsonOptions.Default);
        var ub = await u.Content.ReadFromJsonAsync<UserResponse>(ApiJsonOptions.Default);
        return (workspace.Slug, project.Key, b!.Number, reporter.Id.Value, ub!.Id);
    }

    [Fact]
    public async Task Watch_AddsAndListsWatcher()
    {
        var (ws, proj, n, _, watcher) = await SeedAsync();
        var watchersUrl = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n}/watchers";

        var resp = await _client.PostAsJsonAsync(watchersUrl, new { userId = watcher }, ApiJsonOptions.Default);
        resp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var list = await _client.GetFromJsonAsync<Guid[]>(watchersUrl, ApiJsonOptions.Default);
        list!.Single().ShouldBe(watcher);
    }

    [Fact]
    public async Task Unwatch_RemovesWatcher()
    {
        var (ws, proj, n, _, watcher) = await SeedAsync();
        var watchersUrl = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n}/watchers";
        await _client.PostAsJsonAsync(watchersUrl, new { userId = watcher }, ApiJsonOptions.Default);

        var del = await _client.DeleteAsync($"{watchersUrl}/{watcher}");
        del.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var list = await _client.GetFromJsonAsync<Guid[]>(watchersUrl, ApiJsonOptions.Default);
        list!.Length.ShouldBe(0);
    }

    [Fact]
    public async Task Watch_DuplicateIsIdempotent()
    {
        var (ws, proj, n, _, watcher) = await SeedAsync();
        var url = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n}/watchers";
        await _client.PostAsJsonAsync(url, new { userId = watcher }, ApiJsonOptions.Default);
        var second = await _client.PostAsJsonAsync(url, new { userId = watcher }, ApiJsonOptions.Default);
        second.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var list = await _client.GetFromJsonAsync<Guid[]>(url, ApiJsonOptions.Default);
        list!.Length.ShouldBe(1);
    }

    [Fact]
    public async Task MyWork_IncludesAssignedReportingAndWatching()
    {
        var (ws, proj, _, reporter, watcher) = await SeedAsync();
        // Seed already created one work item where reporter is the reporter.
        // Add a second where watcher is assignee, and have watcher watch a third.
        var itemsUrl = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items";
        var i2 = await _client.PostAsJsonAsync(itemsUrl, new
        {
            tier = "Item", type = "Task", title = "Assigned to watcher",
            reporterId = reporter, assigneeId = watcher
        }, ApiJsonOptions.Default);
        var b2 = await i2.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);

        var i3 = await _client.PostAsJsonAsync(itemsUrl, new
        {
            tier = "Item", type = "Task", title = "Watched by watcher", reporterId = reporter
        }, ApiJsonOptions.Default);
        var b3 = await i3.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);
        await _client.PostAsJsonAsync(
            $"{itemsUrl}/{b3!.Number}/watchers", new { userId = watcher }, ApiJsonOptions.Default);

        var resp = await _client.GetFromJsonAsync<JsonElement>(
            $"/api/v1/users/{watcher}/my-work", ApiJsonOptions.Default);
        resp.GetProperty("assigned").GetArrayLength().ShouldBe(1);
        resp.GetProperty("reporting").GetArrayLength().ShouldBe(0);
        resp.GetProperty("watching").GetArrayLength().ShouldBe(1);
    }
}
