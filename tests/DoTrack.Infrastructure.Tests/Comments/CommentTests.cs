using DoTrack.Application.Comments;
using DoTrack.Domain.Auditing;
using DoTrack.Domain.Comments;
using DoTrack.Domain.Identity;
using DoTrack.Domain.WorkItems;
using DoTrack.Domain.Workspaces;
using DoTrack.Infrastructure.Comments;
using DoTrack.Infrastructure.Persistence;
using DoTrack.Infrastructure.Tests.Builders;
using DoTrack.Infrastructure.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DoTrack.Infrastructure.Tests.Comments;

public abstract class CommentTests<TFixture> : DatabaseTestBase<TFixture>
    where TFixture : class, IDbProviderFixture
{
    protected CommentTests(TFixture fixture) : base(fixture) { }

    private async Task<(WorkItem Item, User Author)> SeedAsync()
    {
        await using var ctx = CreateContext();
        var workspace = WorkspaceBuilder.One();
        var project = new Project(ProjectId.New(), workspace.Id, "PROJ", "Test", null, DateTimeOffset.UtcNow);
        var reporter = UserBuilder.One();
        var item = new WorkItem(
            WorkItemId.New(), project.Id, project.AllocateNextWorkItemNumber(),
            WorkItemTier.Item, WorkItemType.Task, "Sample", null,
            reporter.Id, null, null, DateTimeOffset.UtcNow);
        ctx.Workspaces.Add(workspace);
        ctx.Projects.Add(project);
        ctx.Users.Add(reporter);
        ctx.WorkItems.Add(item);
        await ctx.SaveChangesAsync();
        return (item, reporter);
    }

    [Fact]
    public async Task AddComment_ExternalVisibility_PersistsAndAudits()
    {
        var (item, author) = await SeedAsync();

        await using var ctx = CreateContext();
        await ctx.AuditLogs.ExecuteDeleteAsync(TestContext.Current.CancellationToken);

        var handler = new AddCommentHandler(ctx, TimeProvider.System, new DoTrack.Infrastructure.Outbox.OutboxEmitter(ctx, TimeProvider.System));
        var result = await handler.HandleAsync(
            new AddCommentCommand(item.Id, author.Id, "Looks good to me.", IsInternal: false),
            TestContext.Current.CancellationToken);

        var saved = await ctx.Comments.SingleAsync(c => c.Id == result.Id, TestContext.Current.CancellationToken);
        saved.Body.ShouldBe("Looks good to me.");
        saved.IsInternal.ShouldBeFalse();
        saved.AuthorId.ShouldBe(author.Id);

        var audit = await ctx.AuditLogs.SingleAsync(TestContext.Current.CancellationToken);
        audit.EntityType.ShouldBe("Comment");
        audit.ChangeType.ShouldBe(ChangeType.Insert);
    }

    [Fact]
    public async Task AddComment_InternalFlag_Preserved()
    {
        var (item, author) = await SeedAsync();

        await using var ctx = CreateContext();
        var handler = new AddCommentHandler(ctx, TimeProvider.System, new DoTrack.Infrastructure.Outbox.OutboxEmitter(ctx, TimeProvider.System));
        var result = await handler.HandleAsync(
            new AddCommentCommand(item.Id, author.Id, "Internal note.", IsInternal: true),
            TestContext.Current.CancellationToken);

        var saved = await ctx.Comments.SingleAsync(c => c.Id == result.Id, TestContext.Current.CancellationToken);
        saved.IsInternal.ShouldBeTrue();
    }

    [Fact]
    public async Task AddComment_UnknownWorkItem_Throws()
    {
        var (_, author) = await SeedAsync();

        await using var ctx = CreateContext();
        var handler = new AddCommentHandler(ctx, TimeProvider.System, new DoTrack.Infrastructure.Outbox.OutboxEmitter(ctx, TimeProvider.System));
        var act = () => handler.HandleAsync(
            new AddCommentCommand(WorkItemId.New(), author.Id, "x", false),
            TestContext.Current.CancellationToken);
        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task AddComment_UnicodeAndLongBody_RoundTrip()
    {
        var (item, author) = await SeedAsync();
        var body = "Edge: " + new string('x', 5000) + " 漢字 ñ 🚀";

        await using var ctx = CreateContext();
        var handler = new AddCommentHandler(ctx, TimeProvider.System, new DoTrack.Infrastructure.Outbox.OutboxEmitter(ctx, TimeProvider.System));
        var result = await handler.HandleAsync(
            new AddCommentCommand(item.Id, author.Id, body, false),
            TestContext.Current.CancellationToken);

        var saved = await ctx.Comments.SingleAsync(c => c.Id == result.Id, TestContext.Current.CancellationToken);
        saved.Body.ShouldBe(body);
    }

    [Fact]
    public async Task ListComments_FilterInternal_ExcludesInternalByDefault()
    {
        var (item, author) = await SeedAsync();

        await using var ctx = CreateContext();
        var addHandler = new AddCommentHandler(ctx, TimeProvider.System, new DoTrack.Infrastructure.Outbox.OutboxEmitter(ctx, TimeProvider.System));
        await addHandler.HandleAsync(new AddCommentCommand(item.Id, author.Id, "Public", false), TestContext.Current.CancellationToken);
        await addHandler.HandleAsync(new AddCommentCommand(item.Id, author.Id, "Private", true), TestContext.Current.CancellationToken);

        var listHandler = new ListCommentsHandler(ctx);
        var external = await listHandler.HandleAsync(
            new ListCommentsQuery(item.Id, IncludeInternal: false),
            TestContext.Current.CancellationToken);
        var all = await listHandler.HandleAsync(
            new ListCommentsQuery(item.Id, IncludeInternal: true),
            TestContext.Current.CancellationToken);

        external.Count.ShouldBe(1);
        external[0].Body.ShouldBe("Public");
        all.Count.ShouldBe(2);
    }
}

[Collection(nameof(PostgresCollection))]
public sealed class CommentTests_Postgres(PostgresFixture f) : CommentTests<PostgresFixture>(f);

[Collection(nameof(SqlServerCollection))]
public sealed class CommentTests_SqlServer(SqlServerFixture f) : CommentTests<SqlServerFixture>(f);

[Collection(nameof(SqliteCollection))]
public sealed class CommentTests_Sqlite(SqliteFixture f) : CommentTests<SqliteFixture>(f);
