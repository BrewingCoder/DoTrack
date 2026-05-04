using DoTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Api.Configuration;

public static class DatabaseRegistration
{
    public static IServiceCollection AddConfiguredDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"]
            ?? throw new InvalidOperationException("Configuration 'Database:Provider' is required (postgres | sqlserver | sqlite).");

        var connectionString = configuration["Database:ConnectionString"]
            ?? throw new InvalidOperationException("Configuration 'Database:ConnectionString' is required.");

        return services.AddDoTrackInfrastructure(options =>
        {
            switch (provider.ToLowerInvariant())
            {
                case "postgres":
                case "postgresql":
                    options.UseNpgsql(connectionString,
                        npg => npg.MigrationsAssembly("DoTrack.Migrations.Postgres"));
                    break;

                case "sqlserver":
                case "mssql":
                    options.UseSqlServer(connectionString,
                        sql => sql.MigrationsAssembly("DoTrack.Migrations.SqlServer"));
                    break;

                case "sqlite":
                    options.UseSqlite(connectionString,
                        lite => lite.MigrationsAssembly("DoTrack.Migrations.Sqlite"));
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported database provider '{provider}'. Use one of: postgres, sqlserver, sqlite.");
            }
        });
    }
}
