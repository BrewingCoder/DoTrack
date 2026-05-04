using DoTrack.Application.Sprints;
using DoTrack.Domain.Sprints;
using DoTrack.Domain.WorkItems;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.Sprints;

public sealed class CreateSprintHandler(DoTrackDbContext db, TimeProvider timeProvider) : ICreateSprintHandler
{
    public async Task<CreateSprintResult> HandleAsync(CreateSprintCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var projectExists = await db.Projects.AnyAsync(p => p.Id == command.ProjectId, cancellationToken);
        if (!projectExists)
        {
            throw new InvalidOperationException($"Project '{command.ProjectId.Value}' not found.");
        }

        var sprint = new Sprint(
            SprintId.New(),
            command.ProjectId,
            command.Name,
            command.StartsOn,
            command.EndsOn,
            timeProvider.GetUtcNow());

        db.Sprints.Add(sprint);
        await db.SaveChangesAsync(cancellationToken);
        return new CreateSprintResult(sprint.Id);
    }
}

public sealed class UpdateSprintHandler(DoTrackDbContext db, TimeProvider timeProvider) : IUpdateSprintHandler
{
    public async Task HandleAsync(UpdateSprintCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var sprint = await db.Sprints.SingleOrDefaultAsync(s => s.Id == command.SprintId, cancellationToken)
            ?? throw new InvalidOperationException($"Sprint '{command.SprintId.Value}' not found.");

        var now = timeProvider.GetUtcNow();
        if (command.Name is not null)
        {
            sprint.Rename(command.Name, now);
        }
        if (command.StartsOn is { } start && command.EndsOn is { } end)
        {
            sprint.Reschedule(start, end, now);
        }
        else if (command.StartsOn is not null || command.EndsOn is not null)
        {
            throw new ArgumentException("StartsOn and EndsOn must be supplied together when rescheduling.", nameof(command));
        }
        if (command.State is { } state)
        {
            switch (state)
            {
                case SprintState.Active:
                    sprint.Activate(now);
                    break;
                case SprintState.Completed:
                    sprint.Complete(now);
                    break;
                case SprintState.Planning:
                    // No domain method to revert to Planning; skip.
                    break;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class DeleteSprintHandler(DoTrackDbContext db) : IDeleteSprintHandler
{
    public async Task HandleAsync(DeleteSprintCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var sprint = await db.Sprints.SingleOrDefaultAsync(s => s.Id == command.SprintId, cancellationToken)
            ?? throw new InvalidOperationException($"Sprint '{command.SprintId.Value}' not found.");

        // Clear SprintId on every WorkItem currently assigned (NoAction FK at DB level).
        await db.WorkItems
            .Where(w => w.SprintId == command.SprintId)
            .ExecuteUpdateAsync(s => s.SetProperty(w => w.SprintId, (SprintId?)null), cancellationToken);

        db.Sprints.Remove(sprint);
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ListSprintsHandler(DoTrackDbContext db) : IListSprintsHandler
{
    public async Task<IReadOnlyList<Sprint>> HandleAsync(ListSprintsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var rows = await db.Sprints
            .Where(s => s.ProjectId == query.ProjectId)
            .ToListAsync(cancellationToken);
        return rows.OrderBy(s => s.StartsOn).ToList();
    }
}

public sealed class AssignToSprintHandler(DoTrackDbContext db, TimeProvider timeProvider) : IAssignToSprintHandler
{
    public async Task HandleAsync(AssignToSprintCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var workItem = await db.WorkItems.SingleOrDefaultAsync(w => w.Id == command.WorkItemId, cancellationToken)
            ?? throw new InvalidOperationException($"WorkItem '{command.WorkItemId.Value}' not found.");

        var now = timeProvider.GetUtcNow();
        if (command.SprintId is null)
        {
            workItem.RemoveFromSprint(now);
        }
        else
        {
            var sprint = await db.Sprints.SingleOrDefaultAsync(s => s.Id == command.SprintId.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Sprint '{command.SprintId.Value.Value}' not found.");
            if (sprint.ProjectId != workItem.ProjectId)
            {
                throw new InvalidOperationException("Sprints are project-scoped; cannot assign a work item to a sprint in another project.");
            }
            workItem.AssignToSprint(sprint.Id, now);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ListSprintWorkItemsHandler(DoTrackDbContext db) : IListSprintWorkItemsHandler
{
    public async Task<IReadOnlyList<WorkItem>> HandleAsync(ListSprintWorkItemsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var rows = await db.WorkItems
            .Where(w => w.SprintId == query.SprintId)
            .ToListAsync(cancellationToken);
        return rows.OrderBy(w => w.Number).ToList();
    }
}
