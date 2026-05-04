using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DoTrack.Api.WorkItems;
using DoTrack.Domain.WorkItems;
using DoTrack.Integration.Tests.Json;
using Shouldly;

namespace DoTrack.Integration.Tests.WorkItems;

public sealed class WorkItemEndpointsTests : IClassFixture<DoTrackApiFactory>, IAsyncLifetime
{
    private readonly DoTrackApiFactory _factory;
    private readonly HttpClient _client;

    public WorkItemEndpointsTests(DoTrackApiFactory factory)
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
    public async Task Post_HappyPath_Returns201_WithLocation_AndCorrectBody()
    {
        var (ws, project, reporter) = await _factory.SeedAsync();

        var request = new
        {
            tier = "Item",
            type = "Bug",
            title = "Login fails on iOS",
            description = "Tap login → spinner forever.",
            reporterId = reporter.Id.Value,
            estimatePoints = 3
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workspaces/{ws.Slug}/projects/{project.Key}/work-items",
            request,
            ApiJsonOptions.Default);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location!.ToString().ShouldEndWith($"/work-items/1");

        var body = await response.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);
        body.ShouldNotBeNull();
        body.Key.ShouldBe($"{project.Key}-1");
        body.Number.ShouldBe(1);
        body.Tier.ShouldBe(WorkItemTier.Item);
        body.Type.ShouldBe(WorkItemType.Bug);
        body.State.ShouldBe(WorkItemState.Open);
        body.Title.ShouldBe("Login fails on iOS");
        body.Description.ShouldBe("Tap login → spinner forever.");
        body.EstimatePoints.ShouldBe(3);
        body.ReporterId.ShouldBe(reporter.Id.Value);
        body.AssigneeId.ShouldBeNull();
    }

    [Fact]
    public async Task Post_ConsecutiveCreates_AllocateMonotonicNumbers()
    {
        var (ws, project, reporter) = await _factory.SeedAsync();
        var url = $"/api/v1/workspaces/{ws.Slug}/projects/{project.Key}/work-items";

        var numbers = new List<int>();
        for (var i = 0; i < 3; i++)
        {
            var request = new
            {
                tier = "Item",
                type = "Task",
                title = $"Item {i + 1}",
                reporterId = reporter.Id.Value
            };
            var response = await _client.PostAsJsonAsync(url, request, ApiJsonOptions.Default);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);
            numbers.Add(body!.Number);
        }

        numbers.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task Post_EmptyTitle_Returns400_WithValidationProblem()
    {
        var (ws, project, reporter) = await _factory.SeedAsync();

        var request = new
        {
            tier = "Item",
            type = "Bug",
            title = "",
            reporterId = reporter.Id.Value
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workspaces/{ws.Slug}/projects/{project.Key}/work-items",
            request,
            ApiJsonOptions.Default);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ApiJsonOptions.Default);
        problem.GetProperty("errors").TryGetProperty("title", out var titleErrors).ShouldBeTrue();
        titleErrors.GetArrayLength().ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Post_TierItemWithoutType_Returns400()
    {
        var (ws, project, reporter) = await _factory.SeedAsync();

        var request = new
        {
            tier = "Item",
            type = (string?)null,
            title = "Some work",
            reporterId = reporter.Id.Value
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workspaces/{ws.Slug}/projects/{project.Key}/work-items",
            request,
            ApiJsonOptions.Default);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ApiJsonOptions.Default);
        problem.GetProperty("errors").TryGetProperty("type", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Post_TitleOver512_Returns400()
    {
        var (ws, project, reporter) = await _factory.SeedAsync();

        var request = new
        {
            tier = "Item",
            type = "Task",
            title = new string('a', 513),
            reporterId = reporter.Id.Value
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workspaces/{ws.Slug}/projects/{project.Key}/work-items",
            request,
            ApiJsonOptions.Default);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_NegativeEstimatePoints_Returns400()
    {
        var (ws, project, reporter) = await _factory.SeedAsync();

        var request = new
        {
            tier = "Item",
            type = "Task",
            title = "x",
            reporterId = reporter.Id.Value,
            estimatePoints = -1
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workspaces/{ws.Slug}/projects/{project.Key}/work-items",
            request,
            ApiJsonOptions.Default);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_MissingReporterId_Returns400()
    {
        var (ws, project, _) = await _factory.SeedAsync();

        var request = new
        {
            tier = "Item",
            type = "Task",
            title = "x",
            reporterId = Guid.Empty
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workspaces/{ws.Slug}/projects/{project.Key}/work-items",
            request,
            ApiJsonOptions.Default);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_UnknownProject_Returns404()
    {
        var (ws, _, reporter) = await _factory.SeedAsync();

        var request = new
        {
            tier = "Item",
            type = "Task",
            title = "x",
            reporterId = reporter.Id.Value
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workspaces/{ws.Slug}/projects/NOPE/work-items",
            request,
            ApiJsonOptions.Default);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_UnknownWorkspace_Returns404()
    {
        var (_, project, reporter) = await _factory.SeedAsync();

        var request = new
        {
            tier = "Item",
            type = "Task",
            title = "x",
            reporterId = reporter.Id.Value
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workspaces/no-such-workspace/projects/{project.Key}/work-items",
            request,
            ApiJsonOptions.Default);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_AfterPost_Returns200_WithSameBody()
    {
        var (ws, project, reporter) = await _factory.SeedAsync();
        var url = $"/api/v1/workspaces/{ws.Slug}/projects/{project.Key}/work-items";

        var postBody = new
        {
            tier = "Feature",
            type = (string?)null,
            title = "User-facing feature",
            reporterId = reporter.Id.Value
        };
        var post = await _client.PostAsJsonAsync(url, postBody, ApiJsonOptions.Default);
        var posted = await post.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);

        var get = await _client.GetAsync($"{url}/{posted!.Number}");
        get.StatusCode.ShouldBe(HttpStatusCode.OK);
        var got = await get.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);
        got.ShouldNotBeNull();
        got.Id.ShouldBe(posted.Id);
        got.Tier.ShouldBe(WorkItemTier.Feature);
        got.Type.ShouldBeNull();
        got.Key.ShouldBe($"{project.Key}-{posted.Number}");
    }

    [Fact]
    public async Task Get_UnknownNumber_Returns404()
    {
        var (ws, project, _) = await _factory.SeedAsync();

        var response = await _client.GetAsync(
            $"/api/v1/workspaces/{ws.Slug}/projects/{project.Key}/work-items/999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_TierEpicWithType_Returns201_WithTypeStrippedToNull()
    {
        var (ws, project, reporter) = await _factory.SeedAsync();

        var request = new
        {
            tier = "Epic",
            type = "Bug",
            title = "Big initiative",
            reporterId = reporter.Id.Value
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workspaces/{ws.Slug}/projects/{project.Key}/work-items",
            request,
            ApiJsonOptions.Default);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);
        body!.Tier.ShouldBe(WorkItemTier.Epic);
        body.Type.ShouldBeNull();
    }

    [Fact]
    public async Task Post_ResponseUsesEnumStrings_NotIntegers()
    {
        var (ws, project, reporter) = await _factory.SeedAsync();

        var request = new
        {
            tier = "Item",
            type = "Bug",
            title = "Wire format check",
            reporterId = reporter.Id.Value
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workspaces/{ws.Slug}/projects/{project.Key}/work-items",
            request,
            ApiJsonOptions.Default);

        var raw = await response.Content.ReadAsStringAsync();
        raw.ShouldContain("\"tier\":\"Item\"");
        raw.ShouldContain("\"type\":\"Bug\"");
        raw.ShouldContain("\"state\":\"Open\"");
    }

    [Fact]
    public async Task Post_ProperPath_UsesCamelCaseProperties()
    {
        var (ws, project, reporter) = await _factory.SeedAsync();

        var request = new
        {
            tier = "Item",
            type = "Task",
            title = "Camel case wire check",
            reporterId = reporter.Id.Value
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workspaces/{ws.Slug}/projects/{project.Key}/work-items",
            request,
            ApiJsonOptions.Default);

        var raw = await response.Content.ReadAsStringAsync();
        raw.ShouldContain("\"key\":", Case.Sensitive);
        raw.ShouldContain("\"projectId\":", Case.Sensitive);
        raw.ShouldContain("\"createdAt\":", Case.Sensitive);
        raw.Contains("\"Key\":", StringComparison.Ordinal).ShouldBeFalse();
        raw.Contains("\"ProjectId\":", StringComparison.Ordinal).ShouldBeFalse();
    }
}
