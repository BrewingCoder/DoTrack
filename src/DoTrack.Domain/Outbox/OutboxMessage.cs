using DoTrack.Domain.Auditing;

namespace DoTrack.Domain.Outbox;

public readonly record struct OutboxMessageId(Guid Value)
{
    public static OutboxMessageId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

[NotAudited]
public sealed class OutboxMessage
{
    public OutboxMessageId Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public string ProjectKey { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset? LastAttemptAt { get; private set; }
    public string? LastError { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(
        OutboxMessageId id,
        string eventType,
        string payloadJson,
        string projectKey,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("EventType is required.", nameof(eventType));
        }
        Id = id;
        EventType = eventType;
        PayloadJson = payloadJson;
        ProjectKey = projectKey;
        CreatedAt = now;
    }

    public void MarkDelivered(DateTimeOffset now)
    {
        DeliveredAt = now;
        LastAttemptAt = now;
        LastError = null;
    }

    public void MarkAttemptFailed(string error, DateTimeOffset now)
    {
        Attempts++;
        LastAttemptAt = now;
        LastError = error;
    }
}
