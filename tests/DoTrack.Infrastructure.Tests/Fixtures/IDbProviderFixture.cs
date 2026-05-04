using DoTrack.Application.Abstractions;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.Tests.Fixtures;

public interface IDbProviderFixture : IAsyncLifetime
{
    string ProviderName { get; }

    DbContextOptions<DoTrackDbContext> CreateOptions(
        ICurrentUserAccessor? currentUserAccessor = null,
        IAuditContextAccessor? auditContextAccessor = null,
        TimeProvider? timeProvider = null);
}
