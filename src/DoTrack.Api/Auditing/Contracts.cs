using DoTrack.Domain.Auditing;

namespace DoTrack.Api.Auditing;

public sealed record AuditLogResponse(
    Guid Id,
    string EntityType,
    string EntityId,
    ChangeType ChangeType,
    Guid? ChangedByUserId,
    DateTimeOffset OccurredAt,
    string Source,
    string? ChangeReason,
    IReadOnlyDictionary<string, string>? SourceMetadata,
    IReadOnlyList<FieldChange> FieldChanges);
