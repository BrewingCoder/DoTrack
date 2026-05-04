using DoTrack.Infrastructure.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DoTrack.Infrastructure.Tests.Persistence;

public abstract class MigrationTests<TFixture> : DatabaseTestBase<TFixture>
    where TFixture : class, IDbProviderFixture
{
    protected MigrationTests(TFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Database_CanConnect()
    {
        await using var ctx = CreateContext();
        var canConnect = await ctx.Database.CanConnectAsync();
        canConnect.ShouldBeTrue();
    }

    [Fact]
    public async Task ExpectedTables_Exist_AndAreEmptyAfterClean()
    {
        await using var ctx = CreateContext();
        (await ctx.Users.CountAsync()).ShouldBe(0);
        (await ctx.Workspaces.CountAsync()).ShouldBe(0);
        (await ctx.Projects.CountAsync()).ShouldBe(0);
        (await ctx.WorkItems.CountAsync()).ShouldBe(0);
        (await ctx.WorkItemHierarchies.CountAsync()).ShouldBe(0);
        (await ctx.TimeEntries.CountAsync()).ShouldBe(0);
        (await ctx.Comments.CountAsync()).ShouldBe(0);
        (await ctx.AuditLogs.CountAsync()).ShouldBe(0);
    }
}

[Collection(nameof(PostgresCollection))]
public sealed class MigrationTests_Postgres(PostgresFixture fixture) : MigrationTests<PostgresFixture>(fixture);

[Collection(nameof(SqlServerCollection))]
public sealed class MigrationTests_SqlServer(SqlServerFixture fixture) : MigrationTests<SqlServerFixture>(fixture);

[Collection(nameof(SqliteCollection))]
public sealed class MigrationTests_Sqlite(SqliteFixture fixture) : MigrationTests<SqliteFixture>(fixture);
