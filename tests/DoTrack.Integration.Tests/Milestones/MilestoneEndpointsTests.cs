using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DoTrack.Api.Milestones;
using DoTrack.Api.WorkItems;
using DoTrack.Domain.Milestones;
using DoTrack.Integration.Tests.Json;
using Shouldly;

namespace DoTrack.Integration.Tests.Milestones;

[Collection(nameof(IntegrationCollection))]
public sealed class MilestoneEndpointsTests : IAsyncLifetime
{
    private readonly DoTrackApiFactory _factory;
    private readonly HttpClient _client;

    public MilestoneEndpointsTests(DoTrackApiFactory factory)
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

    private async Task<Guid> CreateMilestoneAsync(string name = "Q2 Launch", decimal? hoursBudget = 200)
    {
        var resp = await _client.PostAsJsonAsync("/api/v1/milestones", new
        {
            name,
            description = "Test",
            targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)),
            hoursBudget,
            visibleToClient = true
        }, ApiJsonOptions.Default);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(ApiJsonOptions.Default);
        return body.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Create_Milestone_ShowsInList()
    {
        var id = await CreateMilestoneAsync();
        var list = await _client.GetFromJsonAsync<MilestoneResponse[]>("/api/v1/milestones", ApiJsonOptions.Default);
        list!.Single().Id.ShouldBe(id);
        list[0].Name.ShouldBe("Q2 Launch");
        list[0].HoursBudget.ShouldBe(200);
    }

    [Fact]
    public async Task Create_BlankName_Returns400()
    {
        var resp = await _client.PostAsJsonAsync("/api/v1/milestones", new
        {
            name = "  ", visibleToClient = false
        }, ApiJsonOptions.Default);
        resp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_State_ToCompleted()
    {
        var id = await CreateMilestoneAsync();
        var resp = await _client.PatchAsJsonAsync($"/api/v1/milestones/{id}", new { state = "Completed" }, ApiJsonOptions.Default);
        resp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var list = await _client.GetFromJsonAsync<MilestoneResponse[]>("/api/v1/milestones", ApiJsonOptions.Default);
        list!.Single().State.ShouldBe(MilestoneState.Completed);
    }

    [Fact]
    public async Task AddScope_AndHealth_ComputesNumbers()
    {
        // Arrange: workspace + project + user via API
        await _client.PostAsJsonAsync("/api/v1/workspaces", new { name = "Acme", slug = "acme" }, ApiJsonOptions.Default);
        await _client.PostAsJsonAsync("/api/v1/workspaces/acme/projects", new { key = "PROJ", name = "P", description = (string?)null }, ApiJsonOptions.Default);
        var userResp = await _client.PostAsJsonAsync("/api/v1/users", new { email = "u@e.com", displayName = "U" }, ApiJsonOptions.Default);
        var userId = (await userResp.Content.ReadFromJsonAsync<JsonElement>(ApiJsonOptions.Default)).GetProperty("id").GetGuid();

        // Two work items: one Accepted, one Open
        var url = "/api/v1/workspaces/acme/projects/PROJ/work-items";
        var i1 = await _client.PostAsJsonAsync(url, new { tier = "Item", type = "Task", title = "Done", reporterId = userId }, ApiJsonOptions.Default);
        var i1body = await i1.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);
        await _client.PatchAsJsonAsync($"{url}/{i1body!.Number}", new { state = "Accepted" }, ApiJsonOptions.Default);

        var i2 = await _client.PostAsJsonAsync(url, new { tier = "Item", type = "Task", title = "Open", reporterId = userId }, ApiJsonOptions.Default);
        var i2body = await i2.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);

        // Log time on both
        await _client.PostAsJsonAsync($"{url}/{i1body.Number}/time-entries", new
        {
            userId, startedAt = DateTimeOffset.UtcNow.AddHours(-3), durationMinutes = 90, description = "On done", billable = true
        }, ApiJsonOptions.Default);
        await _client.PostAsJsonAsync($"{url}/{i2body.Number}/time-entries", new
        {
            userId, startedAt = DateTimeOffset.UtcNow.AddHours(-2), durationMinutes = 60, description = "On open", billable = true
        }, ApiJsonOptions.Default);

        var milestoneId = await CreateMilestoneAsync(name: "Reconciliation", hoursBudget: 5);

        // Add both items to scope
        await _client.PostAsJsonAsync($"/api/v1/milestones/{milestoneId}/scope",
            new { workspaceSlug = "acme", projectKey = "PROJ", number = i1body.Number },
            ApiJsonOptions.Default);
        await _client.PostAsJsonAsync($"/api/v1/milestones/{milestoneId}/scope",
            new { workspaceSlug = "acme", projectKey = "PROJ", number = i2body.Number },
            ApiJsonOptions.Default);

        var health = await _client.GetFromJsonAsync<MilestoneHealthResponse>(
            $"/api/v1/milestones/{milestoneId}/health", ApiJsonOptions.Default);

        health!.ScopeTotal.ShouldBe(2);
        health.ScopeDone.ShouldBe(1);
        health.HoursLogged.ShouldBe(2.5m); // 90 + 60 = 150 min = 2.5 hr
        health.HoursBudget.ShouldBe(5);
        health.BudgetPct.ShouldBe(0.5m);
        health.ScopePct.ShouldBe(0.5m);
        health.HealthGap.ShouldBe(0m);
        health.ProjectedTotal.ShouldBe(5m);
        health.ProjectedOverage.ShouldBe(0m);
    }

    [Fact]
    public async Task RemoveScope_DropsItemFromMilestone()
    {
        await _client.PostAsJsonAsync("/api/v1/workspaces", new { name = "Acme", slug = "acme" }, ApiJsonOptions.Default);
        await _client.PostAsJsonAsync("/api/v1/workspaces/acme/projects", new { key = "PROJ", name = "P", description = (string?)null }, ApiJsonOptions.Default);
        var userResp = await _client.PostAsJsonAsync("/api/v1/users", new { email = "u@e.com", displayName = "U" }, ApiJsonOptions.Default);
        var userId = (await userResp.Content.ReadFromJsonAsync<JsonElement>(ApiJsonOptions.Default)).GetProperty("id").GetGuid();

        var item = await _client.PostAsJsonAsync("/api/v1/workspaces/acme/projects/PROJ/work-items",
            new { tier = "Item", type = "Task", title = "x", reporterId = userId }, ApiJsonOptions.Default);
        var itemBody = await item.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);

        var milestoneId = await CreateMilestoneAsync();
        await _client.PostAsJsonAsync($"/api/v1/milestones/{milestoneId}/scope",
            new { workspaceSlug = "acme", projectKey = "PROJ", number = itemBody!.Number }, ApiJsonOptions.Default);

        var del = await _client.DeleteAsync($"/api/v1/milestones/{milestoneId}/scope/{itemBody.Id}");
        del.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var health = await _client.GetFromJsonAsync<MilestoneHealthResponse>(
            $"/api/v1/milestones/{milestoneId}/health", ApiJsonOptions.Default);
        health!.ScopeTotal.ShouldBe(0);
    }

    [Fact]
    public async Task Delete_Milestone_RemovesScopeAndMilestone()
    {
        var id = await CreateMilestoneAsync();
        var del = await _client.DeleteAsync($"/api/v1/milestones/{id}");
        del.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var list = await _client.GetFromJsonAsync<MilestoneResponse[]>("/api/v1/milestones", ApiJsonOptions.Default);
        list!.Length.ShouldBe(0);
    }
}
