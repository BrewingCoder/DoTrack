using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DoTrack.Migrations.SqlServer;

public sealed class DesignTimeDoTrackDbContextFactory : IDesignTimeDbContextFactory<DoTrackDbContext>
{
    public DoTrackDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DOTRACK_MSSQL_CONNECTION")
            ?? "Server=localhost,1433;Database=dotrack_dev;User Id=sa;Password=D0Track-Dev!;TrustServerCertificate=true";

        var options = new DbContextOptionsBuilder<DoTrackDbContext>()
            .UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(DesignTimeDoTrackDbContextFactory).Assembly.FullName))
            .Options;

        return new DoTrackDbContext(options);
    }
}
