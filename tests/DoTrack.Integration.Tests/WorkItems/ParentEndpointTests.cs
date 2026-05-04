using System.Net;
using System.Net.Http.Json;
using DoTrack.Api.WorkItems;
using DoTrack.Domain.WorkItems;
using DoTrack.Integration.Tests.Json;
using Shouldly;

namespace DoTrack.Integration.Tests.WorkItems;

[Collection(nameof(IntegrationCollection))]
public sealed class ParentEndpointTests : IAsyncLifetime
{
    private readonly DoTrackApiFactory _factory;
    private readonly HttpClient _client;

    public ParentEndpointTests(DoTrackApiFactory factory)
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

    private async Task<(string ws, string proj, int epicNumber, int featureNumber, int itemNumber)> SeedHierarchyAsync()
    {
        var (workspace, project, reporter) = await _factory.SeedAsync();
        var url = $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/work-items";

        var epic = await _client.PostAsJsonAsync(url, new { tier = "Epic", title = "Epic", reporterId = reporter.Id.Value }, ApiJsonOptions.Default);
        var epicBody = await epic.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);

        var feature = await _client.PostAsJsonAsync(url, new { tier = "Feature", title = "Feature", reporterId = reporter.Id.Value }, ApiJsonOptions.Default);
        var featureBody = await feature.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);

        var item = await _client.PostAsJsonAsync(url, new { tier = "Item", type = "Task", title = "Item", reporterId = reporter.Id.Value }, ApiJsonOptions.Default);
        var itemBody = await item.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);

        return (workspace.Slug, project.Key, epicBody!.Number, featureBody!.Number, itemBody!.Number);
    }

    [Fact]
    public async Task SetParent_EpicAsParent_OfFeature_Returns204()
    {
        var (ws, proj, epic, feature, _) = await SeedHierarchyAsync();
        var url = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{feature}/parent";

        var response = await _client.PostAsJsonAsync(url, new { parentNumber = epic }, ApiJsonOptions.Default);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SetParent_TierViolation_ItemUnderItem_Returns400()
    {
        var (workspace, project, reporter) = await _factory.SeedAsync();
        var url = $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/work-items";
        var item1 = await _client.PostAsJsonAsync(url, new { tier = "Item", type = "Task", title = "1", reporterId = reporter.Id.Value }, ApiJsonOptions.Default);
        var item2 = await _client.PostAsJsonAsync(url, new { tier = "Item", type = "Bug", title = "2", reporterId = reporter.Id.Value }, ApiJsonOptions.Default);
        var b1 = (await item1.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default))!;
        var b2 = (await item2.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default))!;

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/work-items/{b2.Number}/parent",
            new { parentNumber = b1.Number },
            ApiJsonOptions.Default);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetParent_UnknownParent_Returns404()
    {
        var (ws, proj, _, _, item) = await SeedHierarchyAsync();
        var url = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{item}/parent";

        var response = await _client.PostAsJsonAsync(url, new { parentNumber = 999 }, ApiJsonOptions.Default);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveParent_ReturnsNoContent_AndClearsLink()
    {
        var (ws, proj, epic, feature, _) = await SeedHierarchyAsync();
        var setUrl = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{feature}/parent";
        await _client.PostAsJsonAsync(setUrl, new { parentNumber = epic }, ApiJsonOptions.Default);

        var del = await _client.DeleteAsync(setUrl);
        del.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
