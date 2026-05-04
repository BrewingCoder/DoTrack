using DoTrack.Application.Abstractions;
using DoTrack.Infrastructure.Auditing;
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
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IAuditContextAccessor, AmbientAuditContextAccessor>();
        services.TryAddDefaultCurrentUserAccessor();
        services.AddScoped<AuditingInterceptor>();

        services.AddDbContext<DoTrackDbContext>((sp, options) =>
        {
            configureDb(options);
            options.AddInterceptors(sp.GetRequiredService<AuditingInterceptor>());
        });

        return services;
    }

    private static void TryAddDefaultCurrentUserAccessor(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserAccessor, NullCurrentUserAccessor>();
    }
}
