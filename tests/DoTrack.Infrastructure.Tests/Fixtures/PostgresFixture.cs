using DoTrack.Application.Abstractions;
using DoTrack.Infrastructure.Auditing;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace DoTrack.Infrastructure.Tests.Fixtures;

public sealed class PostgresFixture : IDbProviderFixture
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("dotrack_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public string ProviderName => "Postgres";

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
            .UseNpgsql(_container.GetConnectionString(), npg =>
                npg.MigrationsAssembly("DoTrack.Migrations.Postgres"))
            .AddInterceptors(interceptor)
            .Options;
    }
}
