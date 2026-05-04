namespace DoTrack.Automation.Abstractions;

public sealed record AutomationEvent
{
    public required Guid EventId { get; init; }
    public required string EventType { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public required string ProjectKey { get; init; }
    public required IReadOnlyDictionary<string, object?> Payload { get; init; }
}
