using DoTrack.Application.Comments;
using DoTrack.Domain.Comments;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.Comments;

public sealed class ListCommentsHandler(DoTrackDbContext db) : IListCommentsHandler
{
    public async Task<IReadOnlyList<Comment>> HandleAsync(ListCommentsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = db.Comments.Where(c => c.WorkItemId == query.WorkItemId);
        if (!query.IncludeInternal)
        {
            q = q.Where(c => !c.IsInternal);
        }

        // Client-side OrderBy: SQLite cannot ORDER BY DateTimeOffset server-side.
        // TODO: monotonic Sequence column on comments before audit/comment UX in v1.
        var rows = await q.ToListAsync(cancellationToken);
        return rows.OrderBy(c => c.CreatedAt).ToList();
    }
}
