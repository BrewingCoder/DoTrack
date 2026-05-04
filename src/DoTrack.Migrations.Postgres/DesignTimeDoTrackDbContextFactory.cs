using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DoTrack.Migrations.Postgres;

public sealed class DesignTimeDoTrackDbContextFactory : IDesignTimeDbContextFactory<DoTrackDbContext>
{
    public DoTrackDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DOTRACK_PG_CONNECTION")
            ?? "Host=localhost;Port=5433;Database=dotrack_dev;Username=dotrack;Password=dotrack";

        var options = new DbContextOptionsBuilder<DoTrackDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(DesignTimeDoTrackDbContextFactory).Assembly.FullName))
            .Options;

        return new DoTrackDbContext(options);
    }
}
