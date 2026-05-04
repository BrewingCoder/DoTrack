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

public abstract class CreateWorkItemHandlerTests<TFixture> : DatabaseTestBase<TFixture>
    where TFixture : class, IDbProviderFixture
{
    protected CreateWorkItemHandlerTests(TFixture fixture) : base(fixture) { }

    private static CreateWorkItemHandler CreateHandler(DoTrackDbContext db, TimeProvider? clock = null)
        => new(db, clock ?? TimeProvider.System);

    private async Task<(Project Project, User Reporter)> SeedProjectAndUserAsync(string projectKey = "PROJ")
    {
        await using var ctx = CreateContext();
        var workspace = WorkspaceBuilder.One();
        var project = new Project(ProjectId.New(), workspace.Id, projectKey, "Test Project", null, DateTimeOffset.UtcNow);
        var reporter = UserBuilder.One();
        ctx.Workspaces.Add(workspace);
        ctx.Projects.Add(project);
        ctx.Users.Add(reporter);
        await ctx.SaveChangesAsync();
        return (project, reporter);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesWorkItemNumberedOne()
    {
        var (project, reporter) = await SeedProjectAndUserAsync();

        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);
        var cmd = new CreateWorkItemCommand(
            project.Id,
            WorkItemTier.Item,
            WorkItemType.Bug,
            "Login button broken on mobile",
            "Steps to reproduce: open on iPhone, tap login.",
            reporter.Id,
            AssigneeId: null,
            EstimatePoints: 3);

        var result = await handler.HandleAsync(cmd, TestContext.Current.CancellationToken);

        result.Number.ShouldBe(1);

        var saved = await ctx.WorkItems.SingleAsync(w => w.Id == result.Id);
        saved.Number.ShouldBe(1);
        saved.Title.ShouldBe("Login button broken on mobile");
        saved.Description.ShouldBe("Steps to reproduce: open on iPhone, tap login.");
        saved.Tier.ShouldBe(WorkItemTier.Item);
        saved.Type.ShouldBe(WorkItemType.Bug);
        saved.EstimatePoints.ShouldBe(3);
        saved.ReporterId.ShouldBe(reporter.Id);
        saved.AssigneeId.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_TwoConsecutiveCreates_IncrementsNumber()
    {
        var (project, reporter) = await SeedProjectAndUserAsync();
        var cmd = new CreateWorkItemCommand(
            project.Id, WorkItemTier.Item, WorkItemType.Task,
            "First", null, reporter.Id, null, null);

        await using (var ctx1 = CreateContext())
        {
            var first = await CreateHandler(ctx1).HandleAsync(cmd, TestContext.Current.CancellationToken);
            first.Number.ShouldBe(1);
        }

        await using (var ctx2 = CreateContext())
        {
            var second = await CreateHandler(ctx2).HandleAsync(cmd with { Title = "Second" }, TestContext.Current.CancellationToken);
            second.Number.ShouldBe(2);
        }

        await using (var ctx3 = CreateContext())
        {
            var third = await CreateHandler(ctx3).HandleAsync(cmd with { Title = "Third" }, TestContext.Current.CancellationToken);
            third.Number.ShouldBe(3);
        }
    }

    [Fact]
    public async Task Handle_DifferentProjects_HaveIndependentSequences()
    {
        var (projA, reporter) = await SeedProjectAndUserAsync("ALPHA");
        await using var setup = CreateContext();
        var workspaceB = WorkspaceBuilder.One();
        var projB = new Project(ProjectId.New(), workspaceB.Id, "BETA", "B", null, DateTimeOffset.UtcNow);
        setup.Workspaces.Add(workspaceB);
        setup.Projects.Add(projB);
        await setup.SaveChangesAsync();

        await using var ctx1 = CreateContext();
        var resultA1 = await CreateHandler(ctx1).HandleAsync(
            new CreateWorkItemCommand(projA.Id, WorkItemTier.Item, WorkItemType.Task, "A1", null, reporter.Id, null, null),
            TestContext.Current.CancellationToken);

        await using var ctx2 = CreateContext();
        var resultB1 = await CreateHandler(ctx2).HandleAsync(
            new CreateWorkItemCommand(projB.Id, WorkItemTier.Item, WorkItemType.Task, "B1", null, reporter.Id, null, null),
            TestContext.Current.CancellationToken);

        await using var ctx3 = CreateContext();
        var resultA2 = await CreateHandler(ctx3).HandleAsync(
            new CreateWorkItemCommand(projA.Id, WorkItemTier.Item, WorkItemType.Task, "A2", null, reporter.Id, null, null),
            TestContext.Current.CancellationToken);

        resultA1.Number.ShouldBe(1);
        resultB1.Number.ShouldBe(1);
        resultA2.Number.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_ProjectMissing_Throws()
    {
        var (_, reporter) = await SeedProjectAndUserAsync();
        var fakeProjectId = ProjectId.New();

        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);
        var cmd = new CreateWorkItemCommand(
            fakeProjectId, WorkItemTier.Item, WorkItemType.Task,
            "Title", null, reporter.Id, null, null);

        var act = () => handler.HandleAsync(cmd, TestContext.Current.CancellationToken);
        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_TierEpic_StripsTypeEvenIfCommandSpecifiesOne()
    {
        var (project, reporter) = await SeedProjectAndUserAsync();

        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);
        var cmd = new CreateWorkItemCommand(
            project.Id, WorkItemTier.Epic, WorkItemType.Bug,
            "Big initiative", null, reporter.Id, null, null);

        var result = await handler.HandleAsync(cmd, TestContext.Current.CancellationToken);
        var saved = await ctx.WorkItems.SingleAsync(w => w.Id == result.Id);
        saved.Tier.ShouldBe(WorkItemTier.Epic);
        saved.Type.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_TierFeature_StripsType()
    {
        var (project, reporter) = await SeedProjectAndUserAsync();

        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);
        var cmd = new CreateWorkItemCommand(
            project.Id, WorkItemTier.Feature, WorkItemType.Story,
            "User-facing feature", null, reporter.Id, null, null);

        var result = await handler.HandleAsync(cmd, TestContext.Current.CancellationToken);
        var saved = await ctx.WorkItems.SingleAsync(w => w.Id == result.Id);
        saved.Tier.ShouldBe(WorkItemTier.Feature);
        saved.Type.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_TierItem_RetainsType()
    {
        var (project, reporter) = await SeedProjectAndUserAsync();

        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);
        var cmd = new CreateWorkItemCommand(
            project.Id, WorkItemTier.Item, WorkItemType.Spike,
            "Investigation", null, reporter.Id, null, null);

        var result = await handler.HandleAsync(cmd, TestContext.Current.CancellationToken);
        var saved = await ctx.WorkItems.SingleAsync(w => w.Id == result.Id);
        saved.Type.ShouldBe(WorkItemType.Spike);
    }

    [Fact]
    public async Task Handle_AuditsWorkItemInsert_ButNotProjectSequenceBump()
    {
        var (project, reporter) = await SeedProjectAndUserAsync();

        await using var ctx = CreateContext();
        // Wipe the audit rows generated during the workspace+project seed so we
        // can isolate exactly what the handler emits.
        await ctx.AuditLogs.ExecuteDeleteAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(ctx);
        var cmd = new CreateWorkItemCommand(
            project.Id, WorkItemTier.Item, WorkItemType.Task,
            "Title", null, reporter.Id, null, null);

        await handler.HandleAsync(cmd, TestContext.Current.CancellationToken);

        var auditRows = await ctx.AuditLogs.ToListAsync(TestContext.Current.CancellationToken);
        // Exactly one audit row for the WorkItem insert.
        // Project's NextWorkItemNumber bump must NOT produce an audit row because
        // the property is [NotAudited] and no other Project property changed.
        auditRows.Count.ShouldBe(1);
        auditRows[0].EntityType.ShouldBe("WorkItem");
        auditRows[0].ChangeType.ShouldBe(ChangeType.Insert);
    }

    [Fact]
    public async Task Handle_AssigneeProvided_PersistsAssigneeId()
    {
        var (project, reporter) = await SeedProjectAndUserAsync();
        var assignee = UserBuilder.One();
        await using (var setup = CreateContext())
        {
            setup.Users.Add(assignee);
            await setup.SaveChangesAsync();
        }

        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);
        var cmd = new CreateWorkItemCommand(
            project.Id, WorkItemTier.Item, WorkItemType.Task,
            "Assigned task", null, reporter.Id, assignee.Id, null);

        var result = await handler.HandleAsync(cmd, TestContext.Current.CancellationToken);
        var saved = await ctx.WorkItems.SingleAsync(w => w.Id == result.Id);
        saved.AssigneeId.ShouldBe(assignee.Id);
    }

    [Fact]
    public async Task Handle_ProjectSequence_PersistsAcrossContexts()
    {
        var (project, reporter) = await SeedProjectAndUserAsync();

        await using (var ctx = CreateContext())
        {
            await CreateHandler(ctx).HandleAsync(
                new CreateWorkItemCommand(project.Id, WorkItemTier.Item, WorkItemType.Task, "First", null, reporter.Id, null, null),
                TestContext.Current.CancellationToken);
        }

        // Reload Project from DB and verify NextWorkItemNumber moved to 2
        await using var verifyCtx = CreateContext();
        var reloaded = await verifyCtx.Projects.SingleAsync(p => p.Id == project.Id);
        reloaded.NextWorkItemNumber.ShouldBe(2);
    }
}

[Collection(nameof(PostgresCollection))]
public sealed class CreateWorkItemHandlerTests_Postgres(PostgresFixture fixture)
    : CreateWorkItemHandlerTests<PostgresFixture>(fixture);

[Collection(nameof(SqlServerCollection))]
public sealed class CreateWorkItemHandlerTests_SqlServer(SqlServerFixture fixture)
    : CreateWorkItemHandlerTests<SqlServerFixture>(fixture);

[Collection(nameof(SqliteCollection))]
public sealed class CreateWorkItemHandlerTests_Sqlite(SqliteFixture fixture)
    : CreateWorkItemHandlerTests<SqliteFixture>(fixture);
