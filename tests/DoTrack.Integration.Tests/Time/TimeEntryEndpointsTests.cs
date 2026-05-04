using System.Net;
using System.Net.Http.Json;
using DoTrack.Api.Time;
using DoTrack.Api.WorkItems;
using DoTrack.Integration.Tests.Json;
using Shouldly;

namespace DoTrack.Integration.Tests.Time;

[Collection(nameof(IntegrationCollection))]
public sealed class TimeEntryEndpointsTests : IAsyncLifetime
{
    private readonly DoTrackApiFactory _factory;
    private readonly HttpClient _client;

    public TimeEntryEndpointsTests(DoTrackApiFactory factory)
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

    private async Task<(string ws, string proj, int n, Guid userId)> SeedItemAsync()
    {
        var (workspace, project, reporter) = await _factory.SeedAsync();
        var url = $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/work-items";
        var resp = await _client.PostAsJsonAsync(url, new { tier = "Item", type = "Task", title = "Sample", reporterId = reporter.Id.Value }, ApiJsonOptions.Default);
        var body = await resp.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);
        return (workspace.Slug, project.Key, body!.Number, reporter.Id.Value);
    }

    [Fact]
    public async Task Log_Time_HappyPath_Returns201()
    {
        var (ws, proj, n, user) = await SeedItemAsync();
        var url = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n}/time-entries";

        var resp = await _client.PostAsJsonAsync(url, new
        {
            userId = user,
            startedAt = DateTimeOffset.UtcNow.AddHours(-1),
            durationMinutes = 45,
            description = "Reviewed PR.",
            billable = true,
            activityType = "Code Review"
        }, ApiJsonOptions.Default);

        resp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<TimeEntryResponse>(ApiJsonOptions.Default);
        body!.DurationMinutes.ShouldBe(45);
        body.Description.ShouldBe("Reviewed PR.");
        body.ActivityType.ShouldBe("Code Review");
    }

    [Fact]
    public async Task Log_Time_BlankDescription_Returns400()
    {
        var (ws, proj, n, user) = await SeedItemAsync();
        var url = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n}/time-entries";

        var resp = await _client.PostAsJsonAsync(url, new
        {
            userId = user,
            startedAt = DateTimeOffset.UtcNow,
            durationMinutes = 30,
            description = "  ",
            billable = false,
            activityType = (string?)null
        }, ApiJsonOptions.Default);

        resp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Log_Time_OverTwentyFourHours_Returns400()
    {
        var (ws, proj, n, user) = await SeedItemAsync();
        var url = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n}/time-entries";

        var resp = await _client.PostAsJsonAsync(url, new
        {
            userId = user,
            startedAt = DateTimeOffset.UtcNow,
            durationMinutes = 25 * 60,
            description = "marathon",
            billable = true,
            activityType = (string?)null
        }, ApiJsonOptions.Default);

        resp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Log_Time_FutureStart_Returns400()
    {
        var (ws, proj, n, user) = await SeedItemAsync();
        var url = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n}/time-entries";

        var resp = await _client.PostAsJsonAsync(url, new
        {
            userId = user,
            startedAt = DateTimeOffset.UtcNow.AddDays(7),
            durationMinutes = 30,
            description = "future",
            billable = false,
            activityType = (string?)null
        }, ApiJsonOptions.Default);

        resp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_TimeEntries_ReturnsLogged()
    {
        var (ws, proj, n, user) = await SeedItemAsync();
        var url = $"/api/v1/workspaces/{ws}/projects/{proj}/work-items/{n}/time-entries";

        var t0 = DateTimeOffset.UtcNow.AddDays(-1);
        await _client.PostAsJsonAsync(url, new { userId = user, startedAt = t0, durationMinutes = 30, description = "a", billable = true, activityType = (string?)null }, ApiJsonOptions.Default);
        await _client.PostAsJsonAsync(url, new { userId = user, startedAt = t0.AddHours(1), durationMinutes = 60, description = "b", billable = true, activityType = (string?)null }, ApiJsonOptions.Default);

        var list = await _client.GetFromJsonAsync<TimeEntryResponse[]>(url, ApiJsonOptions.Default);
        list!.Length.ShouldBe(2);
        list.Select(e => e.Description).ShouldBe(["a", "b"]);
    }
}
