using DoTrack.Application.Abstractions;
using DoTrack.Domain.Auditing;
using DoTrack.Domain.Identity;
using DoTrack.Infrastructure.Tests.Builders;
using DoTrack.Infrastructure.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;

namespace DoTrack.Infrastructure.Tests.Auditing;

public abstract class AuditingTests<TFixture> : DatabaseTestBase<TFixture>
    where TFixture : class, IDbProviderFixture
{
    protected AuditingTests(TFixture fixture) : base(fixture) { }

    [Fact]
    public async Task User_Insert_IsNotAudited()
    {
        await using var ctx = CreateContext();
        ctx.Users.Add(UserBuilder.One());
        await ctx.SaveChangesAsync();

        (await ctx.AuditLogs.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Workspace_Insert_ProducesInsertAuditRow_WithFieldChanges()
    {
        var ws = WorkspaceBuilder.One();

        await using var ctx = CreateContext();
        ctx.Workspaces.Add(ws);
        await ctx.SaveChangesAsync();

        var audit = await ctx.AuditLogs.SingleAsync();
        audit.EntityType.ShouldBe("Workspace");
        audit.EntityId.ShouldBe(ws.Id.Value.ToString());
        audit.ChangeType.ShouldBe(ChangeType.Insert);
        audit.FieldChanges.ShouldContain(fc => fc.FieldName == "Name" && fc.NewValue == ws.Name);
        audit.FieldChanges.ShouldContain(fc => fc.FieldName == "Slug" && fc.NewValue == ws.Slug);
    }

    [Fact]
    public async Task Workspace_Update_ProducesUpdateAuditRow_WithDiff()
    {
        var ws = WorkspaceBuilder.One();

        await using (var ctx1 = CreateContext())
        {
            ctx1.Workspaces.Add(ws);
            await ctx1.SaveChangesAsync();
        }

        await using var ctx2 = CreateContext();
        var loaded = await ctx2.Workspaces.SingleAsync(w => w.Id == ws.Id);
        ctx2.Entry(loaded).Property("Name").CurrentValue = "Renamed Workspace";
        await ctx2.SaveChangesAsync();

        // Client-side OrderBy: SQLite cannot ORDER BY DateTimeOffset server-side.
        // TODO: add monotonic Sequence column to audit_logs before v1 audit UX ships.
        var auditRows = (await ctx2.AuditLogs.ToListAsync())
            .OrderBy(a => a.OccurredAt)
            .ToList();
        auditRows.Count.ShouldBe(2);
        var update = auditRows[1];
        update.ChangeType.ShouldBe(ChangeType.Update);
        update.FieldChanges.Count.ShouldBe(1);
        update.FieldChanges[0].FieldName.ShouldBe("Name");
        update.FieldChanges[0].OldValue.ShouldBe(ws.Name);
        update.FieldChanges[0].NewValue.ShouldBe("Renamed Workspace");
    }

    [Fact]
    public async Task Workspace_Delete_ProducesDeleteAuditRow()
    {
        var ws = WorkspaceBuilder.One();

        await using (var ctx1 = CreateContext())
        {
            ctx1.Workspaces.Add(ws);
            await ctx1.SaveChangesAsync();
        }

        await using var ctx2 = CreateContext();
        ctx2.Workspaces.Remove(await ctx2.Workspaces.SingleAsync(w => w.Id == ws.Id));
        await ctx2.SaveChangesAsync();

        var auditRows = (await ctx2.AuditLogs.ToListAsync())
            .OrderBy(a => a.OccurredAt)
            .ToList();
        auditRows.Count.ShouldBe(2);
        auditRows[1].ChangeType.ShouldBe(ChangeType.Delete);
    }

    [Fact]
    public async Task NoOp_Update_ProducesNoAuditRow()
    {
        var ws = WorkspaceBuilder.One();

        await using (var ctx1 = CreateContext())
        {
            ctx1.Workspaces.Add(ws);
            await ctx1.SaveChangesAsync();
        }

        await using var ctx2 = CreateContext();
        await ctx2.Workspaces.SingleAsync(w => w.Id == ws.Id);
        await ctx2.SaveChangesAsync();

        (await ctx2.AuditLogs.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Audit_CapturesUserId_FromCurrentUserAccessor()
    {
        var userId = UserId.New();
        var accessor = Substitute.For<ICurrentUserAccessor>();
        accessor.CurrentUserId.Returns(userId);

        await using var ctx = CreateContext(currentUserAccessor: accessor);
        ctx.Workspaces.Add(WorkspaceBuilder.One());
        await ctx.SaveChangesAsync();

        var audit = await ctx.AuditLogs.SingleAsync();
        audit.ChangedByUserId.ShouldBe(userId);
    }

    [Fact]
    public async Task Audit_CapturesContextSourceAndReason()
    {
        var contextAccessor = Substitute.For<IAuditContextAccessor>();
        contextAccessor.Current.Returns(new AuditContext("git", Reason: "linked from commit abc1234"));

        await using var ctx = CreateContext(auditContextAccessor: contextAccessor);
        ctx.Workspaces.Add(WorkspaceBuilder.One());
        await ctx.SaveChangesAsync();

        var audit = await ctx.AuditLogs.SingleAsync();
        audit.Source.ShouldBe("git");
        audit.ChangeReason.ShouldBe("linked from commit abc1234");
    }

    [Fact]
    public async Task Audit_DefaultsToSystemSource_WhenContextIsNull()
    {
        await using var ctx = CreateContext();
        ctx.Workspaces.Add(WorkspaceBuilder.One());
        await ctx.SaveChangesAsync();

        var audit = await ctx.AuditLogs.SingleAsync();
        audit.Source.ShouldBe("system");
        audit.ChangedByUserId.ShouldBeNull();
    }
}

[Collection(nameof(PostgresCollection))]
public sealed class AuditingTests_Postgres(PostgresFixture fixture) : AuditingTests<PostgresFixture>(fixture);

[Collection(nameof(SqlServerCollection))]
public sealed class AuditingTests_SqlServer(SqlServerFixture fixture) : AuditingTests<SqlServerFixture>(fixture);

[Collection(nameof(SqliteCollection))]
public sealed class AuditingTests_Sqlite(SqliteFixture fixture) : AuditingTests<SqliteFixture>(fixture);
