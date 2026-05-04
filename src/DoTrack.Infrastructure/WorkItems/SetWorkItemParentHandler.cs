using DoTrack.Application.WorkItems;
using DoTrack.Domain.WorkItems;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.WorkItems;

public sealed class SetWorkItemParentHandler(DoTrackDbContext db) : ISetWorkItemParentHandler
{
    public async Task HandleAsync(SetWorkItemParentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var child = await db.WorkItems.SingleOrDefaultAsync(w => w.Id == command.WorkItemId, cancellationToken)
            ?? throw new InvalidOperationException($"WorkItem '{command.WorkItemId.Value}' not found.");

        if (command.ParentId is null)
        {
            // Remove parent: delete all closure rows where this item is a descendant at depth > 0.
            // Self-row (depth 0) and any rows where this item is the ANCESTOR are preserved.
            await db.WorkItemHierarchies
                .Where(h => h.DescendantId == child.Id && h.Depth > 0)
                .ExecuteDeleteAsync(cancellationToken);
            return;
        }

        var parent = await db.WorkItems.SingleOrDefaultAsync(w => w.Id == command.ParentId.Value, cancellationToken)
            ?? throw new InvalidOperationException($"Parent WorkItem '{command.ParentId.Value.Value}' not found.");

        ValidateTierRule(parent.Tier, child.Tier);
        ValidateCrossProject(parent, child);

        // Cycle check: parent cannot already be a descendant of child.
        var wouldCycle = await db.WorkItemHierarchies
            .AnyAsync(h => h.AncestorId == child.Id && h.DescendantId == parent.Id && h.Depth > 0,
                cancellationToken);
        if (wouldCycle)
        {
            throw new InvalidOperationException("Cycle detected: proposed parent is already a descendant of this work item.");
        }

        await EnsureSelfRowAsync(parent.Id, cancellationToken);
        await EnsureSelfRowAsync(child.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        // Remove any existing depth>0 ancestor rows for this child so we don't accumulate stale links.
        await db.WorkItemHierarchies
            .Where(h => h.DescendantId == child.Id && h.Depth > 0)
            .ExecuteDeleteAsync(cancellationToken);

        // Closure-table insert: for every ancestor A of parent (including parent at depth 0)
        // and every descendant D of child (including child at depth 0), insert (A, D, A.depth + D.depth + 1).
        var parentAncestors = await db.WorkItemHierarchies
            .Where(h => h.DescendantId == parent.Id)
            .ToListAsync(cancellationToken);
        var childDescendants = await db.WorkItemHierarchies
            .Where(h => h.AncestorId == child.Id)
            .ToListAsync(cancellationToken);

        var newRows = parentAncestors
            .SelectMany(superRow => childDescendants.Select(subRow =>
                new WorkItemHierarchy(superRow.AncestorId, subRow.DescendantId, superRow.Depth + subRow.Depth + 1)))
            .ToList();

        db.WorkItemHierarchies.AddRange(newRows);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSelfRowAsync(WorkItemId id, CancellationToken cancellationToken)
    {
        var exists = await db.WorkItemHierarchies
            .AnyAsync(h => h.AncestorId == id && h.DescendantId == id, cancellationToken);
        if (!exists)
        {
            db.WorkItemHierarchies.Add(new WorkItemHierarchy(id, id, 0));
        }
    }

    private static void ValidateTierRule(WorkItemTier parentTier, WorkItemTier childTier)
    {
        var allowed = (parentTier, childTier) switch
        {
            (WorkItemTier.Epic, WorkItemTier.Feature) => true,
            (WorkItemTier.Epic, WorkItemTier.Item) => true,
            (WorkItemTier.Feature, WorkItemTier.Item) => true,
            _ => false
        };
        if (!allowed)
        {
            throw new InvalidOperationException(
                $"Tier rule violation: {childTier} cannot be a child of {parentTier}.");
        }
    }

    private static void ValidateCrossProject(WorkItem parent, WorkItem child)
    {
        if (parent.ProjectId == child.ProjectId)
        {
            return;
        }

        // Cross-project links allowed only at the Epic -> Feature boundary.
        var allowed = parent.Tier == WorkItemTier.Epic && child.Tier == WorkItemTier.Feature;
        if (!allowed)
        {
            throw new InvalidOperationException(
                "Cross-project parent-child links are only allowed Epic -> Feature.");
        }
    }
}
