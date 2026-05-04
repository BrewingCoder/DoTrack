using DoTrack.Domain.WorkItems;

namespace DoTrack.Api.WorkItems;

public sealed record CreateWorkItemRequest(
    WorkItemTier Tier,
    WorkItemType? Type,
    string Title,
    string? Description,
    Guid ReporterId,
    Guid? AssigneeId,
    int? EstimatePoints);

public sealed record WorkItemResponse(
    string Key,
    int Number,
    Guid Id,
    Guid ProjectId,
    WorkItemTier Tier,
    WorkItemType? Type,
    WorkItemState State,
    string Title,
    string? Description,
    Guid ReporterId,
    Guid? AssigneeId,
    int? EstimatePoints,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
