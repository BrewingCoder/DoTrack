using DoTrack.Application.WorkItems;
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

public abstract class SetWorkItemParentHandlerTests<TFixture> : DatabaseTestBase<TFixture>
    where TFixture : class, IDbProviderFixture
{
    protected SetWorkItemParentHandlerTests(TFixture fixture) : base(fixture) { }

    private static SetWorkItemParentHandler CreateHandler(DoTrackDbContext db) => new(db);

    private async Task<(Workspace Ws, Project Project, User Reporter)> SeedAsync(string projectKey = "PROJ")
    {
        await using var ctx = CreateContext();
        var workspace = WorkspaceBuilder.One();
        var project = new Project(ProjectId.New(), workspace.Id, projectKey, "Test", null, DateTimeOffset.UtcNow);
        var reporter = UserBuilder.One();
        ctx.Workspaces.Add(workspace);
        ctx.Projects.Add(project);
        ctx.Users.Add(reporter);
        await ctx.SaveChangesAsync();
        return (workspace, project, reporter);
    }

    private async Task<WorkItem> AddWorkItemAsync(Project project, User reporter, WorkItemTier tier, WorkItemType? type = null)
    {
        await using var ctx = CreateContext();
        var trackedProject = await ctx.Projects.SingleAsync(p => p.Id == project.Id);
        var number = trackedProject.AllocateNextWorkItemNumber();
        var workItem = new WorkItem(
            WorkItemId.New(), project.Id, number, tier,
            tier == WorkItemTier.Item ? (type ?? WorkItemType.Task) : null,
            $"{tier} #{number}", null, reporter.Id, null, null, DateTimeOffset.UtcNow);
        ctx.WorkItems.Add(workItem);
        await ctx.SaveChangesAsync();
        return workItem;
    }

    [Fact]
    public async Task SetParent_EpicAndItem_SameProject_CreatesClosureRows()
    {
        var (_, project, reporter) = await SeedAsync();
        var epic = await AddWorkItemAsync(project, reporter, WorkItemTier.Epic);
        var item = await AddWorkItemAsync(project, reporter, WorkItemTier.Item, WorkItemType.Task);

        await using var ctx = CreateContext();
        await CreateHandler(ctx).HandleAsync(new SetWorkItemParentCommand(item.Id, epic.Id), TestContext.Current.CancellationToken);

        var rows = await ctx.WorkItemHierarchies
            .Where(h => h.AncestorId == epic.Id || h.AncestorId == item.Id || h.DescendantId == epic.Id || h.DescendantId == item.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        rows.ShouldContain(h => h.AncestorId == epic.Id && h.DescendantId == epic.Id && h.Depth == 0);
        rows.ShouldContain(h => h.AncestorId == item.Id && h.DescendantId == item.Id && h.Depth == 0);
        rows.ShouldContain(h => h.AncestorId == epic.Id && h.DescendantId == item.Id && h.Depth == 1);
    }

    [Fact]
    public async Task SetParent_EpicFeatureItem_ProducesDepth2Path()
    {
        var (_, project, reporter) = await SeedAsync();
        var epic = await AddWorkItemAsync(project, reporter, WorkItemTier.Epic);
        var feature = await AddWorkItemAsync(project, reporter, WorkItemTier.Feature);
        var item = await AddWorkItemAsync(project, reporter, WorkItemTier.Item, WorkItemType.Bug);

        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);
        await handler.HandleAsync(new SetWorkItemParentCommand(feature.Id, epic.Id), TestContext.Current.CancellationToken);
        await handler.HandleAsync(new SetWorkItemParentCommand(item.Id, feature.Id), TestContext.Current.CancellationToken);

        var rows = await ctx.WorkItemHierarchies.ToListAsync(TestContext.Current.CancellationToken);
        rows.ShouldContain(h => h.AncestorId == epic.Id && h.DescendantId == feature.Id && h.Depth == 1);
        rows.ShouldContain(h => h.AncestorId == feature.Id && h.DescendantId == item.Id && h.Depth == 1);
        rows.ShouldContain(h => h.AncestorId == epic.Id && h.DescendantId == item.Id && h.Depth == 2);
    }

    [Fact]
    public async Task SetParent_ItemUnderItem_RejectsTierRule()
    {
        var (_, project, reporter) = await SeedAsync();
        var parent = await AddWorkItemAsync(project, reporter, WorkItemTier.Item, WorkItemType.Task);
        var child = await AddWorkItemAsync(project, reporter, WorkItemTier.Item, WorkItemType.Bug);

        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).HandleAsync(
            new SetWorkItemParentCommand(child.Id, parent.Id), TestContext.Current.CancellationToken);
        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SetParent_FeatureUnderItem_RejectsTierRule()
    {
        var (_, project, reporter) = await SeedAsync();
        var item = await AddWorkItemAsync(project, reporter, WorkItemTier.Item, WorkItemType.Task);
        var feature = await AddWorkItemAsync(project, reporter, WorkItemTier.Feature);

        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).HandleAsync(
            new SetWorkItemParentCommand(feature.Id, item.Id), TestContext.Current.CancellationToken);
        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SetParent_FeatureUnderEpic_AcrossProjects_Allowed()
    {
        var (workspace, projectA, reporter) = await SeedAsync("ALPHA");
        await using (var setupCtx = CreateContext())
        {
            var projectB = new Project(ProjectId.New(), workspace.Id, "BETA", "B", null, DateTimeOffset.UtcNow);
            setupCtx.Projects.Add(projectB);
            await setupCtx.SaveChangesAsync();
        }
        await using var loadCtx = CreateContext();
        var projectB2 = await loadCtx.Projects.SingleAsync(p => p.Key == "BETA");

        var epic = await AddWorkItemAsync(projectA, reporter, WorkItemTier.Epic);
        var feature = await AddWorkItemAsync(projectB2, reporter, WorkItemTier.Feature);

        await using var ctx = CreateContext();
        await CreateHandler(ctx).HandleAsync(
            new SetWorkItemParentCommand(feature.Id, epic.Id), TestContext.Current.CancellationToken);

        var hasLink = await ctx.WorkItemHierarchies
            .AnyAsync(h => h.AncestorId == epic.Id && h.DescendantId == feature.Id && h.Depth == 1, TestContext.Current.CancellationToken);
        hasLink.ShouldBeTrue();
    }

    [Fact]
    public async Task SetParent_FeatureUnderEpic_AcrossProjects_NotAtEpicFeature_Rejected()
    {
        var (workspace, projectA, reporter) = await SeedAsync("ALPHA");
        await using (var setupCtx = CreateContext())
        {
            var projectB = new Project(ProjectId.New(), workspace.Id, "BETA", "B", null, DateTimeOffset.UtcNow);
            setupCtx.Projects.Add(projectB);
            await setupCtx.SaveChangesAsync();
        }
        await using var loadCtx = CreateContext();
        var projectB2 = await loadCtx.Projects.SingleAsync(p => p.Key == "BETA");

        var feature = await AddWorkItemAsync(projectA, reporter, WorkItemTier.Feature);
        var item = await AddWorkItemAsync(projectB2, reporter, WorkItemTier.Item, WorkItemType.Task);

        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).HandleAsync(
            new SetWorkItemParentCommand(item.Id, feature.Id), TestContext.Current.CancellationToken);
        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SetParent_Cycle_Rejected()
    {
        var (_, project, reporter) = await SeedAsync();
        var epic = await AddWorkItemAsync(project, reporter, WorkItemTier.Epic);
        var feature = await AddWorkItemAsync(project, reporter, WorkItemTier.Feature);

        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);
        await handler.HandleAsync(new SetWorkItemParentCommand(feature.Id, epic.Id), TestContext.Current.CancellationToken);

        // Reverse: epic under feature would create a cycle
        var act = () => handler.HandleAsync(new SetWorkItemParentCommand(epic.Id, feature.Id), TestContext.Current.CancellationToken);
        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RemoveParent_DeletesAncestorRowsAtDepthGreaterThanZero()
    {
        var (_, project, reporter) = await SeedAsync();
        var epic = await AddWorkItemAsync(project, reporter, WorkItemTier.Epic);
        var feature = await AddWorkItemAsync(project, reporter, WorkItemTier.Feature);

        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);
        await handler.HandleAsync(new SetWorkItemParentCommand(feature.Id, epic.Id), TestContext.Current.CancellationToken);

        await handler.HandleAsync(new SetWorkItemParentCommand(feature.Id, null), TestContext.Current.CancellationToken);

        var hasLink = await ctx.WorkItemHierarchies
            .AnyAsync(h => h.DescendantId == feature.Id && h.Depth > 0, TestContext.Current.CancellationToken);
        hasLink.ShouldBeFalse();

        // Self-row at depth 0 should still exist
        var hasSelfRow = await ctx.WorkItemHierarchies
            .AnyAsync(h => h.AncestorId == feature.Id && h.DescendantId == feature.Id && h.Depth == 0, TestContext.Current.CancellationToken);
        hasSelfRow.ShouldBeTrue();
    }

    [Fact]
    public async Task SetParent_TwiceWithDifferentParent_OldLinkRemoved()
    {
        var (_, project, reporter) = await SeedAsync();
        var epic1 = await AddWorkItemAsync(project, reporter, WorkItemTier.Epic);
        var epic2 = await AddWorkItemAsync(project, reporter, WorkItemTier.Epic);
        var feature = await AddWorkItemAsync(project, reporter, WorkItemTier.Feature);

        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);
        await handler.HandleAsync(new SetWorkItemParentCommand(feature.Id, epic1.Id), TestContext.Current.CancellationToken);
        await handler.HandleAsync(new SetWorkItemParentCommand(feature.Id, epic2.Id), TestContext.Current.CancellationToken);

        var underEpic1 = await ctx.WorkItemHierarchies
            .AnyAsync(h => h.AncestorId == epic1.Id && h.DescendantId == feature.Id, TestContext.Current.CancellationToken);
        var underEpic2 = await ctx.WorkItemHierarchies
            .AnyAsync(h => h.AncestorId == epic2.Id && h.DescendantId == feature.Id, TestContext.Current.CancellationToken);
        underEpic1.ShouldBeFalse();
        underEpic2.ShouldBeTrue();
    }

    [Fact]
    public async Task SetParent_OnItemWithDescendants_DescendantsGetNewAncestors()
    {
        var (_, project, reporter) = await SeedAsync();
        var epic = await AddWorkItemAsync(project, reporter, WorkItemTier.Epic);
        var feature = await AddWorkItemAsync(project, reporter, WorkItemTier.Feature);
        var item = await AddWorkItemAsync(project, reporter, WorkItemTier.Item, WorkItemType.Task);

        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);
        // First: feature has item as child
        await handler.HandleAsync(new SetWorkItemParentCommand(item.Id, feature.Id), TestContext.Current.CancellationToken);
        // Then: epic becomes parent of feature; item should now have epic at depth 2
        await handler.HandleAsync(new SetWorkItemParentCommand(feature.Id, epic.Id), TestContext.Current.CancellationToken);

        var epicToItemDepth = await ctx.WorkItemHierarchies
            .Where(h => h.AncestorId == epic.Id && h.DescendantId == item.Id)
            .Select(h => (int?)h.Depth)
            .SingleOrDefaultAsync(TestContext.Current.CancellationToken);
        epicToItemDepth.ShouldBe(2);
    }
}

[Collection(nameof(PostgresCollection))]
public sealed class SetWorkItemParentHandlerTests_Postgres(PostgresFixture f) : SetWorkItemParentHandlerTests<PostgresFixture>(f);

[Collection(nameof(SqlServerCollection))]
public sealed class SetWorkItemParentHandlerTests_SqlServer(SqlServerFixture f) : SetWorkItemParentHandlerTests<SqlServerFixture>(f);

[Collection(nameof(SqliteCollection))]
public sealed class SetWorkItemParentHandlerTests_Sqlite(SqliteFixture f) : SetWorkItemParentHandlerTests<SqliteFixture>(f);
