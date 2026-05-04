using DoTrack.Application.WorkItems;
using DoTrack.Domain.Auditing;
using DoTrack.Domain.Identity;
using DoTrack.Domain.WorkItems;
using DoTrack.Domain.Workspaces;
using DoTrack.Infrastructure.Persistence;
using DoTrack.Infrastructure.Tests.Builders;
using DoTrack.Infrastructure.Tests.Fixtures;
using DoTrack.Infrastructure.WorkItems;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DoTrack.Infrastructure.Tests.WorkItems;

public abstract class UpdateWorkItemHandlerTests<TFixture> : DatabaseTestBase<TFixture>
    where TFixture : class, IDbProviderFixture
{
    protected UpdateWorkItemHandlerTests(TFixture fixture) : base(fixture) { }

    private static UpdateWorkItemHandler CreateHandler(DoTrackDbContext db, TimeProvider? clock = null)
    {
        var time = clock ?? TimeProvider.System;
        return new UpdateWorkItemHandler(db, time, new DoTrack.Infrastructure.Outbox.OutboxEmitter(db, time));
    }

    private async Task<(Project Project, User Reporter, WorkItem Item)> SeedAsync()
    {
        await using var setup = CreateContext();
        var workspace = WorkspaceBuilder.One();
        var project = new Project(ProjectId.New(), workspace.Id, "PROJ", "Test Project", null, DateTimeOffset.UtcNow);
        var reporter = UserBuilder.One();
        var workItem = new WorkItem(
            WorkItemId.New(), project.Id, project.AllocateNextWorkItemNumber(),
            WorkItemTier.Item, WorkItemType.Task, "Initial Title", "Initial Description",
            reporter.Id, null, 5, DateTimeOffset.UtcNow);
        setup.Workspaces.Add(workspace);
        setup.Projects.Add(project);
        setup.Users.Add(reporter);
        setup.WorkItems.Add(workItem);
        await setup.SaveChangesAsync();
        return (project, reporter, workItem);
    }

    [Fact]
    public async Task Update_TitleOnly_PersistsAndAudits()
    {
        var (_, _, item) = await SeedAsync();

        await using var ctx = CreateContext();
        await ctx.AuditLogs.ExecuteDeleteAsync(TestContext.Current.CancellationToken);

        var cmd = new UpdateWorkItemCommand(item.Id, "Updated Title", null, null, null, null);
        await CreateHandler(ctx).HandleAsync(cmd, TestContext.Current.CancellationToken);

        var reloaded = await ctx.WorkItems.SingleAsync(w => w.Id == item.Id, TestContext.Current.CancellationToken);
        reloaded.Title.ShouldBe("Updated Title");
        reloaded.Description.ShouldBe("Initial Description");

        var audit = await ctx.AuditLogs.SingleAsync(TestContext.Current.CancellationToken);
        audit.ChangeType.ShouldBe(ChangeType.Update);
        audit.FieldChanges.ShouldContain(fc => fc.FieldName == "Title" && fc.NewValue == "Updated Title");
    }

    [Fact]
    public async Task Update_StateTransition_PersistsAndAuditsDiff()
    {
        var (_, _, item) = await SeedAsync();

        await using var ctx = CreateContext();
        await ctx.AuditLogs.ExecuteDeleteAsync(TestContext.Current.CancellationToken);

        var cmd = new UpdateWorkItemCommand(item.Id, null, null, null, null, WorkItemState.InProgress);
        await CreateHandler(ctx).HandleAsync(cmd, TestContext.Current.CancellationToken);

        var reloaded = await ctx.WorkItems.SingleAsync(w => w.Id == item.Id, TestContext.Current.CancellationToken);
        reloaded.State.ShouldBe(WorkItemState.InProgress);

        var audit = await ctx.AuditLogs.SingleAsync(TestContext.Current.CancellationToken);
        audit.FieldChanges.ShouldContain(fc => fc.FieldName == "State" && fc.OldValue == "Open" && fc.NewValue == "InProgress");
    }

    [Fact]
    public async Task Update_AssigneeProvided_PersistsAndAudits()
    {
        var (_, _, item) = await SeedAsync();
        var assignee = UserBuilder.One();
        await using (var setup = CreateContext())
        {
            setup.Users.Add(assignee);
            await setup.SaveChangesAsync();
        }

        await using var ctx = CreateContext();
        await ctx.AuditLogs.ExecuteDeleteAsync(TestContext.Current.CancellationToken);

        var cmd = new UpdateWorkItemCommand(item.Id, null, null, assignee.Id, null, null);
        await CreateHandler(ctx).HandleAsync(cmd, TestContext.Current.CancellationToken);

        var reloaded = await ctx.WorkItems.SingleAsync(w => w.Id == item.Id, TestContext.Current.CancellationToken);
        reloaded.AssigneeId.ShouldBe(assignee.Id);
    }

    [Fact]
    public async Task Update_AllFieldsAtOnce_AppliesAll()
    {
        var (_, _, item) = await SeedAsync();
        var assignee = UserBuilder.One();
        await using (var setup = CreateContext())
        {
            setup.Users.Add(assignee);
            await setup.SaveChangesAsync();
        }

        await using var ctx = CreateContext();
        var cmd = new UpdateWorkItemCommand(
            item.Id,
            "New Title",
            "New Description",
            assignee.Id,
            13,
            WorkItemState.Accepted);
        await CreateHandler(ctx).HandleAsync(cmd, TestContext.Current.CancellationToken);

        var reloaded = await ctx.WorkItems.SingleAsync(w => w.Id == item.Id, TestContext.Current.CancellationToken);
        reloaded.Title.ShouldBe("New Title");
        reloaded.Description.ShouldBe("New Description");
        reloaded.AssigneeId.ShouldBe(assignee.Id);
        reloaded.EstimatePoints.ShouldBe(13);
        reloaded.State.ShouldBe(WorkItemState.Accepted);
    }

    [Fact]
    public async Task Update_NoFieldsProvided_DoesNotChangeAnythingOrAudit()
    {
        var (_, _, item) = await SeedAsync();

        await using var ctx = CreateContext();
        await ctx.AuditLogs.ExecuteDeleteAsync(TestContext.Current.CancellationToken);

        var cmd = new UpdateWorkItemCommand(item.Id, null, null, null, null, null);
        await CreateHandler(ctx).HandleAsync(cmd, TestContext.Current.CancellationToken);

        (await ctx.AuditLogs.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
    }

    [Fact]
    public async Task Update_UnknownWorkItem_Throws()
    {
        await SeedAsync();

        await using var ctx = CreateContext();
        var cmd = new UpdateWorkItemCommand(WorkItemId.New(), "x", null, null, null, null);
        var act = () => CreateHandler(ctx).HandleAsync(cmd, TestContext.Current.CancellationToken);
        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Update_BlankTitle_ThrowsFromDomain()
    {
        var (_, _, item) = await SeedAsync();

        await using var ctx = CreateContext();
        var cmd = new UpdateWorkItemCommand(item.Id, "   ", null, null, null, null);
        var act = () => CreateHandler(ctx).HandleAsync(cmd, TestContext.Current.CancellationToken);
        await act.ShouldThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Update_NegativeEstimate_ThrowsFromDomain()
    {
        var (_, _, item) = await SeedAsync();

        await using var ctx = CreateContext();
        var cmd = new UpdateWorkItemCommand(item.Id, null, null, null, -1, null);
        var act = () => CreateHandler(ctx).HandleAsync(cmd, TestContext.Current.CancellationToken);
        await act.ShouldThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Update_BumpsUpdatedAt()
    {
        var (_, _, item) = await SeedAsync();

        await using var ctx = CreateContext();
        var beforeUpdate = (await ctx.WorkItems.SingleAsync(w => w.Id == item.Id, TestContext.Current.CancellationToken)).UpdatedAt;

        await Task.Delay(20, TestContext.Current.CancellationToken);

        var cmd = new UpdateWorkItemCommand(item.Id, "Touched", null, null, null, null);
        await CreateHandler(ctx).HandleAsync(cmd, TestContext.Current.CancellationToken);

        var after = await ctx.WorkItems.SingleAsync(w => w.Id == item.Id, TestContext.Current.CancellationToken);
        after.UpdatedAt.ShouldBeGreaterThan(beforeUpdate);
    }
}

[Collection(nameof(PostgresCollection))]
public sealed class UpdateWorkItemHandlerTests_Postgres(PostgresFixture fixture)
    : UpdateWorkItemHandlerTests<PostgresFixture>(fixture);

[Collection(nameof(SqlServerCollection))]
public sealed class UpdateWorkItemHandlerTests_SqlServer(SqlServerFixture fixture)
    : UpdateWorkItemHandlerTests<SqlServerFixture>(fixture);

[Collection(nameof(SqliteCollection))]
public sealed class UpdateWorkItemHandlerTests_Sqlite(SqliteFixture fixture)
    : UpdateWorkItemHandlerTests<SqliteFixture>(fixture);
