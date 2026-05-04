using System.Net;
using System.Net.Http.Json;
using DoTrack.Api.Bootstrap;
using DoTrack.Integration.Tests.Json;
using Shouldly;

namespace DoTrack.Integration.Tests.Bootstrap;

[Collection(nameof(IntegrationCollection))]
public sealed class BootstrapEndpointsTests : IAsyncLifetime
{
    private readonly DoTrackApiFactory _factory;
    private readonly HttpClient _client;

    public BootstrapEndpointsTests(DoTrackApiFactory factory)
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
    public async Task Create_Workspace_ShowsInList()
    {
        var resp = await _client.PostAsJsonAsync("/api/v1/workspaces", new { name = "Acme", slug = "acme" }, ApiJsonOptions.Default);
        resp.StatusCode.ShouldBe(HttpStatusCode.Created);

        var list = await _client.GetFromJsonAsync<WorkspaceResponse[]>("/api/v1/workspaces", ApiJsonOptions.Default);
        list!.Single().Slug.ShouldBe("acme");
    }

    [Fact]
    public async Task Create_DuplicateWorkspaceSlug_Returns409()
    {
        await _client.PostAsJsonAsync("/api/v1/workspaces", new { name = "Acme", slug = "acme" }, ApiJsonOptions.Default);
        var second = await _client.PostAsJsonAsync("/api/v1/workspaces", new { name = "Acme2", slug = "acme" }, ApiJsonOptions.Default);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_Project_InWorkspace_AndList()
    {
        await _client.PostAsJsonAsync("/api/v1/workspaces", new { name = "Acme", slug = "acme" }, ApiJsonOptions.Default);
        var resp = await _client.PostAsJsonAsync("/api/v1/workspaces/acme/projects", new { key = "PROJ", name = "First", description = (string?)null }, ApiJsonOptions.Default);
        resp.StatusCode.ShouldBe(HttpStatusCode.Created);

        var list = await _client.GetFromJsonAsync<ProjectResponse[]>("/api/v1/workspaces/acme/projects", ApiJsonOptions.Default);
        list!.Single().Key.ShouldBe("PROJ");
        list[0].NextWorkItemNumber.ShouldBe(1);
    }

    [Fact]
    public async Task Create_DuplicateProjectKey_Returns409()
    {
        await _client.PostAsJsonAsync("/api/v1/workspaces", new { name = "Acme", slug = "acme" }, ApiJsonOptions.Default);
        await _client.PostAsJsonAsync("/api/v1/workspaces/acme/projects", new { key = "PROJ", name = "First", description = (string?)null }, ApiJsonOptions.Default);
        var second = await _client.PostAsJsonAsync("/api/v1/workspaces/acme/projects", new { key = "PROJ", name = "Dup", description = (string?)null }, ApiJsonOptions.Default);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_Project_UnknownWorkspace_Returns404()
    {
        var resp = await _client.PostAsJsonAsync("/api/v1/workspaces/no-such/projects", new { key = "X", name = "n", description = (string?)null }, ApiJsonOptions.Default);
        resp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_User_AndList()
    {
        var resp = await _client.PostAsJsonAsync("/api/v1/users", new { email = "alice@example.com", displayName = "Alice" }, ApiJsonOptions.Default);
        resp.StatusCode.ShouldBe(HttpStatusCode.Created);

        var list = await _client.GetFromJsonAsync<UserResponse[]>("/api/v1/users", ApiJsonOptions.Default);
        list!.Single().Email.ShouldBe("alice@example.com");
    }

    [Fact]
    public async Task Create_DuplicateUserEmail_Returns409()
    {
        await _client.PostAsJsonAsync("/api/v1/users", new { email = "alice@example.com", displayName = "Alice" }, ApiJsonOptions.Default);
        var second = await _client.PostAsJsonAsync("/api/v1/users", new { email = "alice@example.com", displayName = "Other" }, ApiJsonOptions.Default);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task EndToEnd_BootstrapAndCreateWorkItem()
    {
        await _client.PostAsJsonAsync("/api/v1/workspaces", new { name = "Acme", slug = "acme" }, ApiJsonOptions.Default);
        await _client.PostAsJsonAsync("/api/v1/workspaces/acme/projects", new { key = "PROJ", name = "First", description = (string?)null }, ApiJsonOptions.Default);
        var userResp = await _client.PostAsJsonAsync("/api/v1/users", new { email = "alice@example.com", displayName = "Alice" }, ApiJsonOptions.Default);
        var userBody = await userResp.Content.ReadFromJsonAsync<UserResponse>(ApiJsonOptions.Default);

        var item = await _client.PostAsJsonAsync("/api/v1/workspaces/acme/projects/PROJ/work-items",
            new { tier = "Item", type = "Task", title = "Bootstrap test", reporterId = userBody!.Id },
            ApiJsonOptions.Default);
        item.StatusCode.ShouldBe(HttpStatusCode.Created);
    }
}
