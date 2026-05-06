using DoTrack.Application.WorkItems;
using DoTrack.Domain.Identity;
using DoTrack.Domain.WorkItems;
using DoTrack.Domain.Workspaces;

namespace DoTrack.Api.WorkItems;

public static class WorkItemContractMapper
{
    public static CreateWorkItemCommand ToCommand(CreateWorkItemRequest request, ProjectId projectId) => new(
        projectId,
        request.Tier,
        request.Type,
        request.Title,
        request.Description,
        new UserId(request.ReporterId),
        request.AssigneeId is { } assignee ? new UserId(assignee) : null,
        request.EstimatePoints,
        request.Priority);

    public static UpdateWorkItemCommand ToCommand(UpdateWorkItemRequest request, WorkItemId workItemId) => new(
        workItemId,
        request.Title,
        request.Description,
        request.AssigneeId is { } assignee ? new UserId(assignee) : null,
        request.EstimatePoints,
        request.State,
        request.Priority);

    public static WorkItemResponse ToResponse(WorkItem workItem, string projectKey, string? parentKey = null) => new(
        Key: $"{projectKey}-{workItem.Number}",
        Number: workItem.Number,
        Id: workItem.Id.Value,
        ProjectId: workItem.ProjectId.Value,
        Tier: workItem.Tier,
        Type: workItem.Type,
        State: workItem.State,
        Priority: workItem.Priority,
        Title: workItem.Title,
        Description: workItem.Description,
        ReporterId: workItem.ReporterId.Value,
        AssigneeId: workItem.AssigneeId?.Value,
        EstimatePoints: workItem.EstimatePoints,
        ParentKey: parentKey,
        CreatedAt: workItem.CreatedAt,
        UpdatedAt: workItem.UpdatedAt);
}
