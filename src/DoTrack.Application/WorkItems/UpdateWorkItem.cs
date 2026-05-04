using DoTrack.Domain.Identity;
using DoTrack.Domain.WorkItems;

namespace DoTrack.Application.WorkItems;

public sealed record UpdateWorkItemCommand(
    WorkItemId WorkItemId,
    string? Title,
    string? Description,
    UserId? AssigneeId,
    int? EstimatePoints,
    WorkItemState? State);

public sealed record UpdateWorkItemResult(WorkItemId Id);

public interface IUpdateWorkItemHandler
{
    Task<UpdateWorkItemResult> HandleAsync(UpdateWorkItemCommand command, CancellationToken cancellationToken);
}
