using DoTrack.Domain.Sprints;

namespace DoTrack.Api.Sprints;

public sealed record CreateSprintRequest(string Name, DateOnly StartsOn, DateOnly EndsOn);

public sealed record UpdateSprintRequest(
    string? Name,
    DateOnly? StartsOn,
    DateOnly? EndsOn,
    SprintState? State);

public sealed record SprintResponse(
    Guid Id,
    Guid ProjectId,
    string Name,
    DateOnly StartsOn,
    DateOnly EndsOn,
    SprintState State,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AssignToSprintRequest(Guid SprintId);
