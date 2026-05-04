using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DoTrack.Api.Bootstrap;
using DoTrack.Api.SavedQueries;
using DoTrack.Domain.SavedQueries;
using DoTrack.Integration.Tests.Json;
using Shouldly;

namespace DoTrack.Integration.Tests.SavedQueries;

[Collection(nameof(IntegrationCollection))]
public sealed class SavedQueryEndpointsTests : IAsyncLifetime
{
    private readonly DoTrackApiFactory _factory;
    private readonly HttpClient _client;

    public SavedQueryEndpointsTests(DoTrackApiFactory factory)
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

    private async Task<(Guid userId, string ws, string proj)> SeedAsync()
    {
        await _client.PostAsJsonAsync("/api/v1/workspaces", new { name = "Acme", slug = "acme" }, ApiJsonOptions.Default);
        await _client.PostAsJsonAsync("/api/v1/workspaces/acme/projects",
            new { key = "PROJ", name = "p", description = (string?)null }, ApiJsonOptions.Default);
        var u = await _client.PostAsJsonAsync("/api/v1/users",
            new { email = "u@e.com", displayName = "U" }, ApiJsonOptions.Default);
        var ub = await u.Content.ReadFromJsonAsync<UserResponse>(ApiJsonOptions.Default);
        return (ub!.Id, "acme", "PROJ");
    }

    [Fact]
    public async Task Create_PersonalQuery_AppearsInListForOwner()
    {
        var (userId, _, _) = await SeedAsync();
        var resp = await _client.PostAsJsonAsync("/api/v1/saved-queries", new
        {
            ownerUserId = userId,
            scope = "Personal",
            name = "Open bugs",
            queryText = "type:bug state:open",
            color = "#ff0000",
            icon = "bug"
        }, ApiJsonOptions.Default);
        resp.StatusCode.ShouldBe(HttpStatusCode.Created);

        var list = await _client.GetFromJsonAsync<SavedQueryResponse[]>(
            $"/api/v1/saved-queries?userId={userId}", ApiJsonOptions.Default);
        list!.Single().Name.ShouldBe("Open bugs");
        list[0].Scope.ShouldBe(SavedQueryScope.Personal);
    }

    [Fact]
    public async Task Create_ProjectScoped_RequiresProjectKey()
    {
        var (userId, _, _) = await SeedAsync();
        var resp = await _client.PostAsJsonAsync("/api/v1/saved-queries", new
        {
            ownerUserId = userId,
            scope = "Project",
            name = "Project todos",
            queryText = "x"
        }, ApiJsonOptions.Default);
        resp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ProjectScoped_WithKey_ShowsInProjectList()
    {
        var (userId, ws, proj) = await SeedAsync();
        var resp = await _client.PostAsJsonAsync("/api/v1/saved-queries", new
        {
            ownerUserId = userId,
            scope = "Project",
            workspaceSlug = ws,
            projectKey = proj,
            name = "Project todos",
            queryText = "state:open"
        }, ApiJsonOptions.Default);
        resp.StatusCode.ShouldBe(HttpStatusCode.Created);

        var list = await _client.GetFromJsonAsync<SavedQueryResponse[]>(
            $"/api/v1/saved-queries?workspaceSlug={ws}&projectKey={proj}",
            ApiJsonOptions.Default);
        list!.Single().Name.ShouldBe("Project todos");
        list[0].Scope.ShouldBe(SavedQueryScope.Project);
    }

    [Fact]
    public async Task Public_Visible_To_Anyone()
    {
        var (userId, _, _) = await SeedAsync();
        await _client.PostAsJsonAsync("/api/v1/saved-queries", new
        {
            ownerUserId = userId,
            scope = "Public",
            name = "Hot issues",
            queryText = "priority:high"
        }, ApiJsonOptions.Default);

        var list = await _client.GetFromJsonAsync<SavedQueryResponse[]>(
            "/api/v1/saved-queries?includePublic=true", ApiJsonOptions.Default);
        list!.Single().Scope.ShouldBe(SavedQueryScope.Public);
    }

    [Fact]
    public async Task Update_Name_Persists()
    {
        var (userId, _, _) = await SeedAsync();
        var post = await _client.PostAsJsonAsync("/api/v1/saved-queries", new
        {
            ownerUserId = userId, scope = "Personal", name = "Original", queryText = "x"
        }, ApiJsonOptions.Default);
        var id = (await post.Content.ReadFromJsonAsync<JsonElement>(ApiJsonOptions.Default)).GetProperty("id").GetGuid();

        var patch = await _client.PatchAsJsonAsync($"/api/v1/saved-queries/{id}",
            new { name = "Renamed" }, ApiJsonOptions.Default);
        patch.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var list = await _client.GetFromJsonAsync<SavedQueryResponse[]>(
            $"/api/v1/saved-queries?userId={userId}", ApiJsonOptions.Default);
        list!.Single().Name.ShouldBe("Renamed");
    }

    [Fact]
    public async Task Delete_DropsQuery()
    {
        var (userId, _, _) = await SeedAsync();
        var post = await _client.PostAsJsonAsync("/api/v1/saved-queries", new
        {
            ownerUserId = userId, scope = "Personal", name = "Original", queryText = "x"
        }, ApiJsonOptions.Default);
        var id = (await post.Content.ReadFromJsonAsync<JsonElement>(ApiJsonOptions.Default)).GetProperty("id").GetGuid();

        var del = await _client.DeleteAsync($"/api/v1/saved-queries/{id}");
        del.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var list = await _client.GetFromJsonAsync<SavedQueryResponse[]>(
            $"/api/v1/saved-queries?userId={userId}", ApiJsonOptions.Default);
        list!.Length.ShouldBe(0);
    }
}
