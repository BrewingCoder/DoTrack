using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DoTrack.Api.WorkItems;
using DoTrack.Domain.WorkItems;
using DoTrack.Integration.Tests.Json;
using Shouldly;

namespace DoTrack.Integration.Tests.WorkItems;

[Collection(nameof(IntegrationCollection))]
public sealed class WorkItemLinkEndpointsTests : IAsyncLifetime
{
    private readonly DoTrackApiFactory _factory;
    private readonly HttpClient _client;

    public WorkItemLinkEndpointsTests(DoTrackApiFactory factory)
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

    private async Task<(string ws, string proj, int n1, int n2, Guid userId)> SeedTwoItemsAsync()
    {
        var (workspace, project, reporter) = await _factory.SeedAsync();
        var url = $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/work-items";
        var i1 = await _client.PostAsJsonAsync(url, new { tier = "Item", type = "Task", title = "First", reporterId = reporter.Id.Value }, ApiJsonOptions.Default);
        var i2 = await _client.PostAsJsonAsync(url, new { tier = "Item", type = "Bug", title = "Second", reporterId = reporter.Id.Value }, ApiJsonOptions.Default);
        var b1 = (await i1.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default))!;
        var b2 = (await i2.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default))!;
        return (workspace.Slug, project.Key, b1.Number, b2.Number, reporter.Id.Value);
    }

    [Fact]
    public async Task Add_BlocksLink_AppearsOnBothEnds()
    {
        var (ws, proj, n1, n2, _) = await SeedTwoItemsAsync();

        var addUrl = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n1}/links";
        var addResp = await _client.PostAsJsonAsync(addUrl, new
        {
            targetWorkspaceSlug = ws, targetProjectKey = proj, targetNumber = n2, linkType = "Blocks"
        }, ApiJsonOptions.Default);
        addResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        var sourceLinks = await _client.GetFromJsonAsync<WorkItemLinkResponse[]>(addUrl, ApiJsonOptions.Default);
        sourceLinks!.Length.ShouldBe(1);
        sourceLinks[0].LinkType.ShouldBe(WorkItemLinkType.Blocks);
        sourceLinks[0].IsOutbound.ShouldBeTrue();

        var targetUrl = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n2}/links";
        var targetLinks = await _client.GetFromJsonAsync<WorkItemLinkResponse[]>(targetUrl, ApiJsonOptions.Default);
        targetLinks!.Length.ShouldBe(1);
        targetLinks[0].LinkType.ShouldBe(WorkItemLinkType.Blocks);
        targetLinks[0].IsOutbound.ShouldBeFalse();
    }

    [Fact]
    public async Task Add_LinkToSelf_Returns400()
    {
        var (ws, proj, n1, _, _) = await SeedTwoItemsAsync();
        var addUrl = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n1}/links";

        var resp = await _client.PostAsJsonAsync(addUrl, new
        {
            targetWorkspaceSlug = ws, targetProjectKey = proj, targetNumber = n1, linkType = "Relates"
        }, ApiJsonOptions.Default);
        resp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Add_DuplicateLink_Idempotent()
    {
        var (ws, proj, n1, n2, _) = await SeedTwoItemsAsync();
        var addUrl = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n1}/links";
        var body = new { targetWorkspaceSlug = ws, targetProjectKey = proj, targetNumber = n2, linkType = "Duplicates" };

        var first = await _client.PostAsJsonAsync(addUrl, body, ApiJsonOptions.Default);
        var firstId = (await first.Content.ReadFromJsonAsync<JsonElement>(ApiJsonOptions.Default)).GetProperty("id").GetGuid();
        var second = await _client.PostAsJsonAsync(addUrl, body, ApiJsonOptions.Default);
        var secondId = (await second.Content.ReadFromJsonAsync<JsonElement>(ApiJsonOptions.Default)).GetProperty("id").GetGuid();

        firstId.ShouldBe(secondId);

        var links = await _client.GetFromJsonAsync<WorkItemLinkResponse[]>(addUrl, ApiJsonOptions.Default);
        links!.Length.ShouldBe(1);
    }

    [Fact]
    public async Task Remove_Link_DropsIt()
    {
        var (ws, proj, n1, n2, _) = await SeedTwoItemsAsync();
        var addUrl = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n1}/links";
        var add = await _client.PostAsJsonAsync(addUrl, new
        {
            targetWorkspaceSlug = ws, targetProjectKey = proj, targetNumber = n2, linkType = "Causes"
        }, ApiJsonOptions.Default);
        var linkId = (await add.Content.ReadFromJsonAsync<JsonElement>(ApiJsonOptions.Default)).GetProperty("id").GetGuid();

        var del = await _client.DeleteAsync($"/api/v1/work-item-links/{linkId}");
        del.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var links = await _client.GetFromJsonAsync<WorkItemLinkResponse[]>(addUrl, ApiJsonOptions.Default);
        links!.Length.ShouldBe(0);
    }
}
