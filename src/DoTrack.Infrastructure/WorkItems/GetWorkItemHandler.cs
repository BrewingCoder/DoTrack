using DoTrack.Application.WorkItems;
using DoTrack.Domain.WorkItems;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.WorkItems;

public sealed class GetWorkItemHandler(DoTrackDbContext db) : IGetWorkItemHandler
{
    public async Task<WorkItem?> HandleAsync(GetWorkItemQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await db.WorkItems
            .SingleOrDefaultAsync(
                w => w.ProjectId == query.ProjectId && w.Number == query.Number,
                cancellationToken);
    }
}

public sealed class ListWorkItemsForProjectHandler(DoTrackDbContext db) : IListWorkItemsForProjectHandler
{
    public async Task<IReadOnlyList<WorkItem>> HandleAsync(ListWorkItemsForProjectQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var rows = await db.WorkItems
            .Where(w => w.ProjectId == query.ProjectId)
            .ToListAsync(cancellationToken);
        return rows.OrderBy(w => w.Number).ToList();
    }
}
