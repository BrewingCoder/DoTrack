using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DoTrack.Migrations.Sqlite;

public sealed class DesignTimeDoTrackDbContextFactory : IDesignTimeDbContextFactory<DoTrackDbContext>
{
    public DoTrackDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DOTRACK_SQLITE_CONNECTION")
            ?? "Data Source=/tmp/dotrack-dev.db";

        var options = new DbContextOptionsBuilder<DoTrackDbContext>()
            .UseSqlite(connectionString, sqlite =>
                sqlite.MigrationsAssembly(typeof(DesignTimeDoTrackDbContextFactory).Assembly.FullName))
            .Options;

        return new DoTrackDbContext(options);
    }
}
