using DoTrack.Application.Auditing;
using DoTrack.Domain.Auditing;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.Auditing;

public sealed class GetEntityHistoryHandler(DoTrackDbContext db) : IGetEntityHistoryHandler
{
    public async Task<IReadOnlyList<AuditLog>> HandleAsync(GetEntityHistoryQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // SQLite cannot ORDER BY DateTimeOffset server-side, so we sort client-side.
        // We page-limit at the query level with Take to bound the in-memory load.
        var rows = await db.AuditLogs
            .Where(a => a.EntityType == query.EntityType && a.EntityId == query.EntityId)
            .Take(query.Limit)
            .ToListAsync(cancellationToken);
        return rows.OrderByDescending(a => a.OccurredAt).ToList();
    }
}
