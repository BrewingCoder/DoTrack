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
        request.EstimatePoints);

    public static WorkItemResponse ToResponse(WorkItem workItem, string projectKey) => new(
        Key: $"{projectKey}-{workItem.Number}",
        Number: workItem.Number,
        Id: workItem.Id.Value,
        ProjectId: workItem.ProjectId.Value,
        Tier: workItem.Tier,
        Type: workItem.Type,
        State: workItem.State,
        Title: workItem.Title,
        Description: workItem.Description,
        ReporterId: workItem.ReporterId.Value,
        AssigneeId: workItem.AssigneeId?.Value,
        EstimatePoints: workItem.EstimatePoints,
        CreatedAt: workItem.CreatedAt,
        UpdatedAt: workItem.UpdatedAt);
}
