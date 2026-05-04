using DoTrack.Application.WorkItems;
using DoTrack.Domain.WorkItems;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.WorkItems;

public sealed class AddWorkItemLinkHandler(DoTrackDbContext db, TimeProvider timeProvider) : IAddWorkItemLinkHandler
{
    public async Task<AddWorkItemLinkResult> HandleAsync(AddWorkItemLinkCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.SourceId == command.TargetId)
        {
            throw new ArgumentException("Cannot link a work item to itself.");
        }

        var sourceExists = await db.WorkItems.AnyAsync(w => w.Id == command.SourceId, cancellationToken);
        if (!sourceExists)
        {
            throw new InvalidOperationException($"Source WorkItem '{command.SourceId.Value}' not found.");
        }
        var targetExists = await db.WorkItems.AnyAsync(w => w.Id == command.TargetId, cancellationToken);
        if (!targetExists)
        {
            throw new InvalidOperationException($"Target WorkItem '{command.TargetId.Value}' not found.");
        }

        // Idempotency: same source/target/type only adds once
        var existing = await db.WorkItemLinks
            .Where(l => l.SourceId == command.SourceId && l.TargetId == command.TargetId && l.LinkType == command.LinkType)
            .Select(l => l.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing != default)
        {
            return new AddWorkItemLinkResult(existing);
        }

        var link = new WorkItemLink(
            WorkItemLinkId.New(), command.SourceId, command.TargetId, command.LinkType,
            command.CreatedByUserId, timeProvider.GetUtcNow());
        db.WorkItemLinks.Add(link);
        await db.SaveChangesAsync(cancellationToken);
        return new AddWorkItemLinkResult(link.Id);
    }
}

public sealed class RemoveWorkItemLinkHandler(DoTrackDbContext db) : IRemoveWorkItemLinkHandler
{
    public async Task HandleAsync(RemoveWorkItemLinkCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await db.WorkItemLinks
            .Where(l => l.Id == command.LinkId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}

public sealed class ListWorkItemLinksHandler(DoTrackDbContext db) : IListWorkItemLinksHandler
{
    public async Task<IReadOnlyList<WorkItemLinkView>> HandleAsync(ListWorkItemLinksQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var outbound = await db.WorkItemLinks
            .Where(l => l.SourceId == query.WorkItemId)
            .Select(l => new WorkItemLinkView(l.Id, l.TargetId, l.LinkType, true, l.CreatedAt))
            .ToListAsync(cancellationToken);
        var inbound = await db.WorkItemLinks
            .Where(l => l.TargetId == query.WorkItemId)
            .Select(l => new WorkItemLinkView(l.Id, l.SourceId, l.LinkType, false, l.CreatedAt))
            .ToListAsync(cancellationToken);

        return outbound.Concat(inbound).OrderBy(v => v.CreatedAt).ToList();
    }
}
