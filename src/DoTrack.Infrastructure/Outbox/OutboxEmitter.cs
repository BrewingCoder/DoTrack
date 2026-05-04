using System.Text.Json;
using DoTrack.Domain.Outbox;
using DoTrack.Infrastructure.Persistence;

namespace DoTrack.Infrastructure.Outbox;

/// <summary>
/// Helper for handlers to emit OutboxMessages alongside the entity changes
/// that triggered them. Caller is responsible for the SaveChanges that commits
/// both the domain change and the queued event in the same transaction.
/// </summary>
public sealed class OutboxEmitter(DoTrackDbContext db, TimeProvider timeProvider)
{
    public void Emit(string eventType, string projectKey, object payload)
    {
        ArgumentException.ThrowIfNullOrEmpty(eventType);
        ArgumentNullException.ThrowIfNull(payload);

        var json = JsonSerializer.Serialize(payload);
        var message = new OutboxMessage(
            OutboxMessageId.New(),
            eventType,
            json,
            projectKey,
            timeProvider.GetUtcNow());
        db.OutboxMessages.Add(message);
    }
}
