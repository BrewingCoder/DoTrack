namespace DoTrack.Api.Time;

public sealed record LogTimeRequest(
    Guid UserId,
    DateTimeOffset StartedAt,
    int DurationMinutes,
    string Description,
    bool Billable,
    string? ActivityType);

public sealed record TimeEntryResponse(
    Guid Id,
    Guid WorkItemId,
    Guid UserId,
    DateTimeOffset StartedAt,
    int DurationMinutes,
    string Description,
    bool Billable,
    string? ActivityType,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastEditedAt);
