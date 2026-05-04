using DoTrack.Domain.WorkItems;

namespace DoTrack.Api.AcceptanceCriteria;

public sealed record AddCriterionRequest(string Description);

public sealed record UpdateCriterionStatusRequest(
    AcceptanceCriterionStatus Status,
    Guid UserId,
    string? Comment);

public sealed record AcceptanceCriterionResponse(
    Guid Id,
    Guid WorkItemId,
    string Description,
    AcceptanceCriterionStatus Status,
    Guid? CheckedByUserId,
    DateTimeOffset? CheckedAt,
    string? Comment,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
