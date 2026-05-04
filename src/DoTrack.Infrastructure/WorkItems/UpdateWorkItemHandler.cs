using DoTrack.Application.WorkItems;
using DoTrack.Domain.WorkItems;
using DoTrack.Infrastructure.Outbox;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.WorkItems;

public sealed class UpdateWorkItemHandler(DoTrackDbContext db, TimeProvider timeProvider, OutboxEmitter outbox) : IUpdateWorkItemHandler
{
    public async Task<UpdateWorkItemResult> HandleAsync(UpdateWorkItemCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var workItem = await db.WorkItems.SingleOrDefaultAsync(w => w.Id == command.WorkItemId, cancellationToken)
            ?? throw new InvalidOperationException($"WorkItem '{command.WorkItemId.Value}' not found.");

        var now = timeProvider.GetUtcNow();
        var prevState = workItem.State;
        var prevAssignee = workItem.AssigneeId;

        if (command.Title is not null)
        {
            workItem.UpdateTitle(command.Title, now);
        }
        if (command.Description is not null)
        {
            workItem.UpdateDescription(command.Description, now);
        }
        if (command.AssigneeId is { } assignee)
        {
            workItem.Assign(assignee, now);
        }
        if (command.EstimatePoints is not null)
        {
            workItem.SetEstimate(command.EstimatePoints, now);
        }
        if (command.State is { } newState)
        {
            workItem.TransitionState(newState, now);
        }

        var project = await db.Projects.SingleAsync(p => p.Id == workItem.ProjectId, cancellationToken);

        if (prevState != workItem.State)
        {
            outbox.Emit("issue.state_changed", project.Key, new
            {
                workItemId = workItem.Id.Value,
                projectKey = project.Key,
                number = workItem.Number,
                key = $"{project.Key}-{workItem.Number}",
                fromState = prevState.ToString(),
                toState = workItem.State.ToString(),
                occurredAt = now
            });
        }
        if (prevAssignee != workItem.AssigneeId)
        {
            outbox.Emit("issue.assigned", project.Key, new
            {
                workItemId = workItem.Id.Value,
                projectKey = project.Key,
                number = workItem.Number,
                key = $"{project.Key}-{workItem.Number}",
                fromAssigneeId = prevAssignee?.Value,
                toAssigneeId = workItem.AssigneeId?.Value,
                occurredAt = now
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return new UpdateWorkItemResult(workItem.Id);
    }
}
