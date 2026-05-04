using DoTrack.Application.WorkItems;
using DoTrack.Domain.WorkItems;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.WorkItems;

public sealed class CreateWorkItemHandler(DoTrackDbContext db, TimeProvider timeProvider) : ICreateWorkItemHandler
{
    public async Task<CreateWorkItemResult> HandleAsync(CreateWorkItemCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var project = await db.Projects.SingleOrDefaultAsync(p => p.Id == command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project '{command.ProjectId.Value}' not found.");

        var number = project.AllocateNextWorkItemNumber();

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
            timeProvider.GetUtcNow());

        db.WorkItems.Add(workItem);
        await db.SaveChangesAsync(cancellationToken);

        return new CreateWorkItemResult(workItem.Id, number);
    }
}
