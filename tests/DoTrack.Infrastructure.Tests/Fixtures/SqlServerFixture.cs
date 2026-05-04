using DoTrack.Application.Abstractions;
using DoTrack.Infrastructure.Auditing;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace DoTrack.Infrastructure.Tests.Fixtures;

public sealed class SqlServerFixture : IDbProviderFixture
{
    private readonly MsSqlContainer _container =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public string ProviderName => "SqlServer";

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        var options = CreateOptions();
        await using var ctx = new DoTrackDbContext(options);
        await ctx.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public DbContextOptions<DoTrackDbContext> CreateOptions(
        ICurrentUserAccessor? currentUserAccessor = null,
        IAuditContextAccessor? auditContextAccessor = null,
        TimeProvider? timeProvider = null)
    {
        var interceptor = new AuditingInterceptor(
            currentUserAccessor ?? new NullCurrentUserAccessor(),
            auditContextAccessor ?? new AmbientAuditContextAccessor(),
            timeProvider ?? TimeProvider.System);

        return new DbContextOptionsBuilder<DoTrackDbContext>()
            .UseSqlServer(_container.GetConnectionString(), sql =>
                sql.MigrationsAssembly("DoTrack.Migrations.SqlServer"))
            .AddInterceptors(interceptor)
            .Options;
    }
}
