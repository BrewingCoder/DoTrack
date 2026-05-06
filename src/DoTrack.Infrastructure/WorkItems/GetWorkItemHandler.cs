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
    public async Task<IReadOnlyList<ListedWorkItem>> HandleAsync(ListWorkItemsForProjectQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var items = await db.WorkItems
            .Where(w => w.ProjectId == query.ProjectId)
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return Array.Empty<ListedWorkItem>();
        }

        // Direct-parent lookup via the closure table: depth=1 ancestor of each
        // listed item, joined to WorkItems + Projects for the prefixed key.
        var itemIds = items.Select(w => w.Id).ToHashSet();
        var parentRows = await (
            from h in db.WorkItemHierarchies
            where h.Depth == 1 && itemIds.Contains(h.DescendantId)
            join parent in db.WorkItems on h.AncestorId equals parent.Id
            join project in db.Projects on parent.ProjectId equals project.Id
            select new { h.DescendantId, ParentKey = project.Key + "-" + parent.Number })
            .ToListAsync(cancellationToken);

        var parentByDescendant = parentRows.ToDictionary(r => r.DescendantId, r => r.ParentKey);

        return items
            .OrderBy(w => w.Number)
            .Select(w => new ListedWorkItem(w, parentByDescendant.GetValueOrDefault(w.Id)))
            .ToList();
    }
}
