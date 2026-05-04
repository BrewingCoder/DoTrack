using DoTrack.Application.Abstractions;
using DoTrack.Infrastructure.Persistence;
using DoTrack.Infrastructure.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.Tests;

public abstract class DatabaseTestBase<TFixture> : IAsyncLifetime
    where TFixture : class, IDbProviderFixture
{
    protected TFixture Fixture { get; }

    protected DatabaseTestBase(TFixture fixture)
    {
        Fixture = fixture;
    }

    protected DoTrackDbContext CreateContext(
        ICurrentUserAccessor? currentUserAccessor = null,
        IAuditContextAccessor? auditContextAccessor = null,
        TimeProvider? timeProvider = null)
        => new(Fixture.CreateOptions(currentUserAccessor, auditContextAccessor, timeProvider));

    public virtual async ValueTask InitializeAsync()
    {
        await using var ctx = CreateContext();
        await CleanTablesAsync(ctx);
    }

    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static async Task CleanTablesAsync(DoTrackDbContext ctx)
    {
        await ctx.AuditLogs.ExecuteDeleteAsync();
        await ctx.Comments.ExecuteDeleteAsync();
        await ctx.TimeEntries.ExecuteDeleteAsync();
        await ctx.WorkItemHierarchies.ExecuteDeleteAsync();
        await ctx.WorkItems.ExecuteDeleteAsync();
        await ctx.Projects.ExecuteDeleteAsync();
        await ctx.Workspaces.ExecuteDeleteAsync();
        await ctx.Users.ExecuteDeleteAsync();
    }
}
