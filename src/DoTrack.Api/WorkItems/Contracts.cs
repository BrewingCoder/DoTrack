using DoTrack.Domain.WorkItems;

namespace DoTrack.Api.WorkItems;

public sealed record CreateWorkItemRequest(
    WorkItemTier Tier,
    WorkItemType? Type,
    string Title,
    string? Description,
    Guid ReporterId,
    Guid? AssigneeId,
    int? EstimatePoints,
    WorkItemPriority? Priority);

// PATCH semantics: null/missing means "no change." Once a work item is
// assigned, it cannot be unassigned via this endpoint; same for clearing
// description. A future v0.1 endpoint will add explicit clear/unassign.
public sealed record UpdateWorkItemRequest(
    string? Title,
    string? Description,
    Guid? AssigneeId,
    int? EstimatePoints,
    WorkItemState? State,
    WorkItemPriority? Priority);

// Body for POST /work-items/{n}/parent. The parent is identified by its
// number within a project — same workspace+project by default, or specify
// the cross-project Epic->Feature case via ParentProjectKey/ParentWorkspaceSlug.
public sealed record SetParentRequest(
    int ParentNumber,
    string? ParentProjectKey,
    string? ParentWorkspaceSlug);

public sealed record WorkItemResponse(
    string Key,
    int Number,
    Guid Id,
    Guid ProjectId,
    WorkItemTier Tier,
    WorkItemType? Type,
    WorkItemState State,
    WorkItemPriority Priority,
    string Title,
    string? Description,
    Guid ReporterId,
    Guid? AssigneeId,
    int? EstimatePoints,
    string? ParentKey,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
