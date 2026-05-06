using DoTrack.Application.Time;
using DoTrack.Domain.Auditing;
using DoTrack.Domain.Identity;
using DoTrack.Domain.WorkItems;
using DoTrack.Domain.Workspaces;
using DoTrack.Infrastructure.Persistence;
using DoTrack.Infrastructure.Tests.Builders;
using DoTrack.Infrastructure.Tests.Fixtures;
using DoTrack.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DoTrack.Infrastructure.Tests.Time;

public abstract class TimeEntryTests<TFixture> : DatabaseTestBase<TFixture>
    where TFixture : class, IDbProviderFixture
{
    protected TimeEntryTests(TFixture fixture) : base(fixture) { }

    private async Task<(WorkItem Item, User User)> SeedAsync()
    {
        await using var ctx = CreateContext();
        var workspace = WorkspaceBuilder.One();
        var project = new Project(ProjectId.New(), workspace.Id, "PROJ", "Test", null, DateTimeOffset.UtcNow);
        var user = UserBuilder.One();
        var item = new WorkItem(
            WorkItemId.New(), project.Id, project.AllocateNextWorkItemNumber(),
            WorkItemTier.Item, WorkItemType.Task, "Sample", null,
            user.Id, null, null, WorkItemPriority.Normal, DateTimeOffset.UtcNow);
        ctx.Workspaces.Add(workspace);
        ctx.Projects.Add(project);
        ctx.Users.Add(user);
        ctx.WorkItems.Add(item);
        await ctx.SaveChangesAsync();
        return (item, user);
    }

    [Fact]
    public async Task LogTime_HappyPath_PersistsAndAudits()
    {
        var (item, user) = await SeedAsync();

        await using var ctx = CreateContext();
        await ctx.AuditLogs.ExecuteDeleteAsync(TestContext.Current.CancellationToken);

        var handler = new LogTimeHandler(ctx, TimeProvider.System, new DoTrack.Infrastructure.Outbox.OutboxEmitter(ctx, TimeProvider.System));
        var result = await handler.HandleAsync(
            new LogTimeCommand(item.Id, user.Id, DateTimeOffset.UtcNow.AddHours(-2), TimeSpan.FromMinutes(90),
                "Wrote tests for create handler.", Billable: true, ActivityType: "Development"),
            TestContext.Current.CancellationToken);

        var saved = await ctx.TimeEntries.SingleAsync(t => t.Id == result.Id, TestContext.Current.CancellationToken);
        saved.Duration.ShouldBe(TimeSpan.FromMinutes(90));
        saved.Description.ShouldBe("Wrote tests for create handler.");
        saved.Billable.ShouldBeTrue();
        saved.ActivityType.ShouldBe("Development");

        var audit = await ctx.AuditLogs.SingleAsync(TestContext.Current.CancellationToken);
        audit.EntityType.ShouldBe("TimeEntry");
        audit.ChangeType.ShouldBe(ChangeType.Insert);
    }

    [Fact]
    public async Task LogTime_ZeroDuration_Throws()
    {
        var (item, user) = await SeedAsync();

        await using var ctx = CreateContext();
        var handler = new LogTimeHandler(ctx, TimeProvider.System, new DoTrack.Infrastructure.Outbox.OutboxEmitter(ctx, TimeProvider.System));
        var act = () => handler.HandleAsync(
            new LogTimeCommand(item.Id, user.Id, DateTimeOffset.UtcNow, TimeSpan.Zero, "x", false, null),
            TestContext.Current.CancellationToken);
        await act.ShouldThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task LogTime_NegativeDuration_Throws()
    {
        var (item, user) = await SeedAsync();

        await using var ctx = CreateContext();
        var handler = new LogTimeHandler(ctx, TimeProvider.System, new DoTrack.Infrastructure.Outbox.OutboxEmitter(ctx, TimeProvider.System));
        var act = () => handler.HandleAsync(
            new LogTimeCommand(item.Id, user.Id, DateTimeOffset.UtcNow, TimeSpan.FromMinutes(-30), "x", false, null),
            TestContext.Current.CancellationToken);
        await act.ShouldThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task LogTime_BlankDescription_Throws()
    {
        var (item, user) = await SeedAsync();

        await using var ctx = CreateContext();
        var handler = new LogTimeHandler(ctx, TimeProvider.System, new DoTrack.Infrastructure.Outbox.OutboxEmitter(ctx, TimeProvider.System));
        var act = () => handler.HandleAsync(
            new LogTimeCommand(item.Id, user.Id, DateTimeOffset.UtcNow, TimeSpan.FromMinutes(15), "  ", false, null),
            TestContext.Current.CancellationToken);
        await act.ShouldThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task LogTime_UnknownWorkItem_Throws()
    {
        var (_, user) = await SeedAsync();

        await using var ctx = CreateContext();
        var handler = new LogTimeHandler(ctx, TimeProvider.System, new DoTrack.Infrastructure.Outbox.OutboxEmitter(ctx, TimeProvider.System));
        var act = () => handler.HandleAsync(
            new LogTimeCommand(WorkItemId.New(), user.Id, DateTimeOffset.UtcNow, TimeSpan.FromMinutes(15), "x", false, null),
            TestContext.Current.CancellationToken);
        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ListTimeEntries_ReturnsInChronologicalOrder()
    {
        var (item, user) = await SeedAsync();

        await using var ctx = CreateContext();
        var log = new LogTimeHandler(ctx, TimeProvider.System, new DoTrack.Infrastructure.Outbox.OutboxEmitter(ctx, TimeProvider.System));
        var t0 = DateTimeOffset.UtcNow.AddDays(-3);
        await log.HandleAsync(new LogTimeCommand(item.Id, user.Id, t0.AddHours(2), TimeSpan.FromMinutes(30), "second", false, null), TestContext.Current.CancellationToken);
        await log.HandleAsync(new LogTimeCommand(item.Id, user.Id, t0, TimeSpan.FromMinutes(30), "first", false, null), TestContext.Current.CancellationToken);
        await log.HandleAsync(new LogTimeCommand(item.Id, user.Id, t0.AddHours(4), TimeSpan.FromMinutes(30), "third", false, null), TestContext.Current.CancellationToken);

        var list = new ListTimeEntriesHandler(ctx);
        var entries = await list.HandleAsync(new ListTimeEntriesQuery(item.Id), TestContext.Current.CancellationToken);

        entries.Select(e => e.Description).ShouldBe(["first", "second", "third"]);
    }
}

[Collection(nameof(PostgresCollection))]
public sealed class TimeEntryTests_Postgres(PostgresFixture f) : TimeEntryTests<PostgresFixture>(f);

[Collection(nameof(SqlServerCollection))]
public sealed class TimeEntryTests_SqlServer(SqlServerFixture f) : TimeEntryTests<SqlServerFixture>(f);

[Collection(nameof(SqliteCollection))]
public sealed class TimeEntryTests_Sqlite(SqliteFixture f) : TimeEntryTests<SqliteFixture>(f);
