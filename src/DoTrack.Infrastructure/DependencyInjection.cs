using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DoTrack.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDoTrackInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddDbContext<DoTrackDbContext>(configureDb);
        return services;
    }
}
