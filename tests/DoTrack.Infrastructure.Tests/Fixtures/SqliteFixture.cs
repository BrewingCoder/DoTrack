using DoTrack.Application.Abstractions;
using DoTrack.Infrastructure.Auditing;
using DoTrack.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.Tests.Fixtures;

public sealed class SqliteFixture : IDbProviderFixture
{
    private SqliteConnection? _connection;

    public string ProviderName => "Sqlite";

    public async ValueTask InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();
        var options = CreateOptions();
        await using var ctx = new DoTrackDbContext(options);
        await ctx.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }

    public DbContextOptions<DoTrackDbContext> CreateOptions(
        ICurrentUserAccessor? currentUserAccessor = null,
        IAuditContextAccessor? auditContextAccessor = null,
        TimeProvider? timeProvider = null)
    {
        if (_connection is null)
        {
            throw new InvalidOperationException("SqliteFixture not initialized.");
        }

        var interceptor = new AuditingInterceptor(
            currentUserAccessor ?? new NullCurrentUserAccessor(),
            auditContextAccessor ?? new AmbientAuditContextAccessor(),
            timeProvider ?? TimeProvider.System);

        return new DbContextOptionsBuilder<DoTrackDbContext>()
            .UseSqlite(_connection, sqlite =>
                sqlite.MigrationsAssembly("DoTrack.Migrations.Sqlite"))
            .AddInterceptors(interceptor)
            .Options;
    }
}
