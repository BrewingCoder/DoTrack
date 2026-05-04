using DoTrack.Application.Time;
using DoTrack.Domain.Time;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.Time;

public sealed class ListTimeEntriesHandler(DoTrackDbContext db) : IListTimeEntriesHandler
{
    public async Task<IReadOnlyList<TimeEntry>> HandleAsync(ListTimeEntriesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Client-side OrderBy: SQLite cannot ORDER BY DateTimeOffset server-side.
        var rows = await db.TimeEntries
            .Where(t => t.WorkItemId == query.WorkItemId)
            .ToListAsync(cancellationToken);
        return rows.OrderBy(t => t.StartedAt).ToList();
    }
}
