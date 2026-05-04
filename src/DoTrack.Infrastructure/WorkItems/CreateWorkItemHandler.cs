using DoTrack.Application.WorkItems;
using DoTrack.Domain.WorkItems;
using DoTrack.Infrastructure.Outbox;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.WorkItems;

public sealed class CreateWorkItemHandler(DoTrackDbContext db, TimeProvider timeProvider, OutboxEmitter outbox) : ICreateWorkItemHandler
{
    public async Task<CreateWorkItemResult> HandleAsync(CreateWorkItemCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var project = await db.Projects.SingleOrDefaultAsync(p => p.Id == command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project '{command.ProjectId.Value}' not found.");

        var number = project.AllocateNextWorkItemNumber();

        var now = timeProvider.GetUtcNow();
        var workItem = new WorkItem(
            WorkItemId.New(),
            project.Id,
            number,
            command.Tier,
            command.Tier == WorkItemTier.Item ? command.Type : null,
            command.Title,
            command.Description,
            command.ReporterId,
            command.AssigneeId,
            command.EstimatePoints,
            now);

        db.WorkItems.Add(workItem);

        outbox.Emit("issue.created", project.Key, new
        {
            workItemId = workItem.Id.Value,
            projectKey = project.Key,
            number,
            key = $"{project.Key}-{number}",
            tier = workItem.Tier.ToString(),
            type = workItem.Type?.ToString(),
            title = workItem.Title,
            state = workItem.State.ToString(),
            reporterId = workItem.ReporterId.Value,
            assigneeId = workItem.AssigneeId?.Value,
            estimatePoints = workItem.EstimatePoints,
            createdAt = workItem.CreatedAt
        });

        await db.SaveChangesAsync(cancellationToken);

        return new CreateWorkItemResult(workItem.Id, number);
    }
}
