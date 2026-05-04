using DoTrack.Domain.Identity;

namespace DoTrack.Domain.Auditing;

public readonly record struct AuditLogId(Guid Value)
{
    public static AuditLogId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public enum ChangeType
{
    Insert = 1,
    Update = 2,
    Delete = 3
}

public readonly record struct FieldChange(string FieldName, string? OldValue, string? NewValue);

[NotAudited]
public sealed class AuditLog
{
    public AuditLogId Id { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public ChangeType ChangeType { get; private set; }
    public UserId? ChangedByUserId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public string? ChangeReason { get; private set; }
    public string? SourceMetadataJson { get; private set; }
    public IReadOnlyList<FieldChange> FieldChanges { get; private set; } = Array.Empty<FieldChange>();

    private AuditLog() { }

    public AuditLog(
        AuditLogId id,
        string entityType,
        string entityId,
        ChangeType changeType,
        UserId? changedByUserId,
        DateTimeOffset occurredAt,
        string source,
        string? changeReason,
        string? sourceMetadataJson,
        IReadOnlyList<FieldChange> fieldChanges)
    {
        Id = id;
        EntityType = entityType;
        EntityId = entityId;
        ChangeType = changeType;
        ChangedByUserId = changedByUserId;
        OccurredAt = occurredAt;
        Source = source;
        ChangeReason = changeReason;
        SourceMetadataJson = sourceMetadataJson;
        FieldChanges = fieldChanges;
    }
}
