using DoTrack.Domain.Identity;
using DoTrack.Domain.WorkItems;
using DoTrack.Domain.Workspaces;

namespace DoTrack.Application.WorkItems;

public sealed record CreateWorkItemCommand(
    ProjectId ProjectId,
    WorkItemTier Tier,
    WorkItemType? Type,
    string Title,
    string? Description,
    UserId ReporterId,
    UserId? AssigneeId,
    int? EstimatePoints,
    WorkItemPriority? Priority = null);

public sealed record CreateWorkItemResult(WorkItemId Id, int Number);

public interface ICreateWorkItemHandler
{
    Task<CreateWorkItemResult> HandleAsync(CreateWorkItemCommand command, CancellationToken cancellationToken);
}
