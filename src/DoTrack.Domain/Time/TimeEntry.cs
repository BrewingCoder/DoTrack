using DoTrack.Domain.Identity;
using DoTrack.Domain.WorkItems;

namespace DoTrack.Domain.Time;

public readonly record struct TimeEntryId(Guid Value)
{
    public static TimeEntryId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public sealed class TimeEntry
{
    public TimeEntryId Id { get; private set; }
    public WorkItemId WorkItemId { get; private set; }
    public UserId UserId { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public TimeSpan Duration { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public bool Billable { get; private set; }
    public string? ActivityType { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastEditedAt { get; private set; }

    private TimeEntry() { }

    public TimeEntry(
        TimeEntryId id,
        WorkItemId workItemId,
        UserId userId,
        DateTimeOffset startedAt,
        TimeSpan duration,
        string description,
        bool billable,
        string? activityType,
        DateTimeOffset now)
    {
        Id = id;
        WorkItemId = workItemId;
        UserId = userId;
        StartedAt = startedAt;
        Duration = duration;
        Description = description;
        Billable = billable;
        ActivityType = activityType;
        CreatedAt = now;
    }
}
