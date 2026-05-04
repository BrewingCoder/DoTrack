using DoTrack.Application.WorkItems;
using DoTrack.Domain.Identity;
using DoTrack.Domain.WorkItems;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.WorkItems;

public sealed class WatchWorkItemHandler(DoTrackDbContext db, TimeProvider timeProvider) : IWatchWorkItemHandler
{
    public async Task HandleAsync(WatchWorkItemCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var workItemExists = await db.WorkItems.AnyAsync(w => w.Id == command.WorkItemId, cancellationToken);
        if (!workItemExists)
        {
            throw new InvalidOperationException($"WorkItem '{command.WorkItemId.Value}' not found.");
        }
        var userExists = await db.Users.AnyAsync(u => u.Id == command.UserId, cancellationToken);
        if (!userExists)
        {
            throw new InvalidOperationException($"User '{command.UserId.Value}' not found.");
        }
        var already = await db.WorkItemWatchers
            .AnyAsync(w => w.WorkItemId == command.WorkItemId && w.UserId == command.UserId, cancellationToken);
        if (already)
        {
            return;
        }
        db.WorkItemWatchers.Add(new WorkItemWatcher(command.WorkItemId, command.UserId, timeProvider.GetUtcNow()));
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class UnwatchWorkItemHandler(DoTrackDbContext db) : IUnwatchWorkItemHandler
{
    public async Task HandleAsync(UnwatchWorkItemCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await db.WorkItemWatchers
            .Where(w => w.WorkItemId == command.WorkItemId && w.UserId == command.UserId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}

public sealed class ListWatchersHandler(DoTrackDbContext db) : IListWatchersHandler
{
    public async Task<IReadOnlyList<UserId>> HandleAsync(ListWatchersQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await db.WorkItemWatchers
            .Where(w => w.WorkItemId == query.WorkItemId)
            .Select(w => w.UserId)
            .ToListAsync(cancellationToken);
    }
}

public sealed class MyWorkHandler(DoTrackDbContext db) : IMyWorkHandler
{
    public async Task<MyWorkResult> HandleAsync(MyWorkQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var assigned = await db.WorkItems
            .Where(w => w.AssigneeId == query.UserId && w.State != WorkItemState.Accepted)
            .ToListAsync(cancellationToken);
        var reporting = await db.WorkItems
            .Where(w => w.ReporterId == query.UserId && w.State != WorkItemState.Accepted)
            .ToListAsync(cancellationToken);
        var watchedIds = await db.WorkItemWatchers
            .Where(w => w.UserId == query.UserId)
            .Select(w => w.WorkItemId)
            .ToListAsync(cancellationToken);
        var watching = watchedIds.Count == 0
            ? new List<WorkItem>()
            : await db.WorkItems
                .Where(w => watchedIds.Contains(w.Id) && w.State != WorkItemState.Accepted)
                .ToListAsync(cancellationToken);
        return new MyWorkResult(
            assigned.OrderByDescending(w => w.UpdatedAt).ToList(),
            reporting.OrderByDescending(w => w.UpdatedAt).ToList(),
            watching.OrderByDescending(w => w.UpdatedAt).ToList());
    }
}
