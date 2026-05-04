using DoTrack.Application.WorkItems;
using DoTrack.Domain.Identity;
using DoTrack.Domain.WorkItems;
using DoTrack.Domain.Workspaces;
using DoTrack.Infrastructure.Tests.Builders;
using DoTrack.Infrastructure.Tests.Fixtures;
using DoTrack.Infrastructure.WorkItems;
using Shouldly;

namespace DoTrack.Infrastructure.Tests.WorkItems;

public abstract class ListWorkItemsForProjectHandlerTests<TFixture> : DatabaseTestBase<TFixture>
    where TFixture : class, IDbProviderFixture
{
    protected ListWorkItemsForProjectHandlerTests(TFixture fixture) : base(fixture) { }

    private async Task<(Project Project, User Reporter)> SeedProjectAsync(string projectKey = "PROJ")
    {
        await using var ctx = CreateContext();
        var workspace = WorkspaceBuilder.One();
        var project = new Project(ProjectId.New(), workspace.Id, projectKey, $"Test Project {projectKey}", null, DateTimeOffset.UtcNow);
        var reporter = UserBuilder.One();
        ctx.Workspaces.Add(workspace);
        ctx.Projects.Add(project);
        ctx.Users.Add(reporter);
        await ctx.SaveChangesAsync();
        return (project, reporter);
    }

    private static WorkItem MakeItem(
        ProjectId projectId,
        UserId reporterId,
        int number,
        string title,
        WorkItemTier tier = WorkItemTier.Item,
        WorkItemType? type = WorkItemType.Task) => new(
            WorkItemId.New(),
            projectId,
            number,
            tier,
            tier == WorkItemTier.Item ? type : null,
            title,
            null,
            reporterId,
            null,
            null,
            DateTimeOffset.UtcNow);

    [Fact]
    public async Task Handle_NoItems_ReturnsEmpty()
    {
        var (project, _) = await SeedProjectAsync();

        await using var ctx = CreateContext();
        var handler = new ListWorkItemsForProjectHandler(ctx);

        var result = await handler.HandleAsync(
            new ListWorkItemsForProjectQuery(project.Id),
            TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_NonexistentProject_ReturnsEmpty()
    {
        await using var ctx = CreateContext();
        var handler = new ListWorkItemsForProjectHandler(ctx);

        var result = await handler.HandleAsync(
            new ListWorkItemsForProjectQuery(ProjectId.New()),
            TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_OneItem_ReturnsOne()
    {
        var (project, reporter) = await SeedProjectAsync();
        await using (var seed = CreateContext())
        {
            seed.WorkItems.Add(MakeItem(project.Id, reporter.Id, 1, "Only item"));
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var ctx = CreateContext();
        var result = await new ListWorkItemsForProjectHandler(ctx).HandleAsync(
            new ListWorkItemsForProjectQuery(project.Id),
            TestContext.Current.CancellationToken);

        result.Count.ShouldBe(1);
        result[0].Title.ShouldBe("Only item");
        result[0].Number.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_MultipleItems_OrderedByNumberAscending()
    {
        var (project, reporter) = await SeedProjectAsync();
        await using (var seed = CreateContext())
        {
            // Insert out of order to prove ordering happens in the handler, not by chance.
            seed.WorkItems.Add(MakeItem(project.Id, reporter.Id, 3, "Third"));
            seed.WorkItems.Add(MakeItem(project.Id, reporter.Id, 1, "First"));
            seed.WorkItems.Add(MakeItem(project.Id, reporter.Id, 2, "Second"));
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var ctx = CreateContext();
        var result = await new ListWorkItemsForProjectHandler(ctx).HandleAsync(
            new ListWorkItemsForProjectQuery(project.Id),
            TestContext.Current.CancellationToken);

        result.Select(w => w.Number).ShouldBe([1, 2, 3]);
        result.Select(w => w.Title).ShouldBe(["First", "Second", "Third"]);
    }

    [Fact]
    public async Task Handle_OtherProjectItems_NotIncluded()
    {
        var (projectA, reporter) = await SeedProjectAsync("ALPHA");
        await using var setup = CreateContext();
        var workspaceB = WorkspaceBuilder.One();
        var projectB = new Project(ProjectId.New(), workspaceB.Id, "BETA", "B", null, DateTimeOffset.UtcNow);
        setup.Workspaces.Add(workspaceB);
        setup.Projects.Add(projectB);
        setup.WorkItems.Add(MakeItem(projectA.Id, reporter.Id, 1, "A-1"));
        setup.WorkItems.Add(MakeItem(projectB.Id, reporter.Id, 1, "B-1"));
        setup.WorkItems.Add(MakeItem(projectB.Id, reporter.Id, 2, "B-2"));
        await setup.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var ctx = CreateContext();
        var resultA = await new ListWorkItemsForProjectHandler(ctx).HandleAsync(
            new ListWorkItemsForProjectQuery(projectA.Id),
            TestContext.Current.CancellationToken);

        resultA.Count.ShouldBe(1);
        resultA[0].Title.ShouldBe("A-1");
        resultA.ShouldAllBe(w => w.ProjectId == projectA.Id);
    }

    [Fact]
    public async Task Handle_AllTiersPresent_AllReturned()
    {
        var (project, reporter) = await SeedProjectAsync();
        await using (var seed = CreateContext())
        {
            seed.WorkItems.Add(MakeItem(project.Id, reporter.Id, 1, "Epic", tier: WorkItemTier.Epic));
            seed.WorkItems.Add(MakeItem(project.Id, reporter.Id, 2, "Feature", tier: WorkItemTier.Feature));
            seed.WorkItems.Add(MakeItem(project.Id, reporter.Id, 3, "Item", tier: WorkItemTier.Item, type: WorkItemType.Bug));
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var ctx = CreateContext();
        var result = await new ListWorkItemsForProjectHandler(ctx).HandleAsync(
            new ListWorkItemsForProjectQuery(project.Id),
            TestContext.Current.CancellationToken);

        result.Count.ShouldBe(3);
        result.Select(w => w.Tier).ShouldBe([WorkItemTier.Epic, WorkItemTier.Feature, WorkItemTier.Item]);
    }

    [Fact]
    public async Task Handle_ManyItems_AllReturnedInOrder()
    {
        var (project, reporter) = await SeedProjectAsync();
        const int count = 50;
        await using (var seed = CreateContext())
        {
            for (var n = count; n >= 1; n--)
            {
                seed.WorkItems.Add(MakeItem(project.Id, reporter.Id, n, $"Item {n}"));
            }
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var ctx = CreateContext();
        var result = await new ListWorkItemsForProjectHandler(ctx).HandleAsync(
            new ListWorkItemsForProjectQuery(project.Id),
            TestContext.Current.CancellationToken);

        result.Count.ShouldBe(count);
        result.Select(w => w.Number).ShouldBe(Enumerable.Range(1, count));
    }
}

[Collection(nameof(PostgresCollection))]
public sealed class ListWorkItemsForProjectHandlerTests_Postgres(PostgresFixture fixture)
    : ListWorkItemsForProjectHandlerTests<PostgresFixture>(fixture);

[Collection(nameof(SqlServerCollection))]
public sealed class ListWorkItemsForProjectHandlerTests_SqlServer(SqlServerFixture fixture)
    : ListWorkItemsForProjectHandlerTests<SqlServerFixture>(fixture);

[Collection(nameof(SqliteCollection))]
public sealed class ListWorkItemsForProjectHandlerTests_Sqlite(SqliteFixture fixture)
    : ListWorkItemsForProjectHandlerTests<SqliteFixture>(fixture);
