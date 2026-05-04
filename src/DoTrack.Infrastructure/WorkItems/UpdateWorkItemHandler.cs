using DoTrack.Application.WorkItems;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.WorkItems;

public sealed class UpdateWorkItemHandler(DoTrackDbContext db, TimeProvider timeProvider) : IUpdateWorkItemHandler
{
    public async Task<UpdateWorkItemResult> HandleAsync(UpdateWorkItemCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var workItem = await db.WorkItems.SingleOrDefaultAsync(w => w.Id == command.WorkItemId, cancellationToken)
            ?? throw new InvalidOperationException($"WorkItem '{command.WorkItemId.Value}' not found.");

        var now = timeProvider.GetUtcNow();

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

        await db.SaveChangesAsync(cancellationToken);
        return new UpdateWorkItemResult(workItem.Id);
    }
}
