using System.Net.Http.Json;
using System.Text.Json;
using DoTrack.Api.WorkItems;
using DoTrack.Infrastructure.Persistence;
using DoTrack.Integration.Tests.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DoTrack.Integration.Tests.Outbox;

[Collection(nameof(IntegrationCollection))]
public sealed class OutboxEmissionTests : IAsyncLifetime
{
    private readonly DoTrackApiFactory _factory;
    private readonly HttpClient _client;

    public OutboxEmissionTests(DoTrackApiFactory factory)
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
    public async Task CreateWorkItem_EmitsIssueCreatedOutboxMessage()
    {
        var (workspace, project, reporter) = await _factory.SeedAsync();

        var url = $"/api/v1/workspaces/{workspace.Slug}/projects/{project.Key}/work-items";
        var post = await _client.PostAsJsonAsync(url, new
        {
            tier = "Item", type = "Bug", title = "Outbox test", reporterId = reporter.Id.Value
        }, ApiJsonOptions.Default);
        post.EnsureSuccessStatusCode();
        var body = await post.Content.ReadFromJsonAsync<WorkItemResponse>(ApiJsonOptions.Default);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DoTrackDbContext>();
        var messages = await db.OutboxMessages.ToListAsync();
        var msg = messages.SingleOrDefault(m => m.EventType == "issue.created");

        msg.ShouldNotBeNull();
        msg.DeliveredAt.ShouldBeNull();
        msg.Attempts.ShouldBe(0);
        msg.ProjectKey.ShouldBe(project.Key);

        using var doc = JsonDocument.Parse(msg.PayloadJson);
        doc.RootElement.GetProperty("key").GetString().ShouldBe($"{project.Key}-{body!.Number}");
        doc.RootElement.GetProperty("tier").GetString().ShouldBe("Item");
        doc.RootElement.GetProperty("type").GetString().ShouldBe("Bug");
        doc.RootElement.GetProperty("title").GetString().ShouldBe("Outbox test");
    }
}
