using DoTrack.Domain.Identity;

namespace DoTrack.Domain.WorkItems;

public sealed class WorkItemWatcher
{
    public WorkItemId WorkItemId { get; private set; }
    public UserId UserId { get; private set; }
    public DateTimeOffset AddedAt { get; private set; }

    private WorkItemWatcher() { }

    public WorkItemWatcher(WorkItemId workItemId, UserId userId, DateTimeOffset now)
    {
        WorkItemId = workItemId;
        UserId = userId;
        AddedAt = now;
    }
}
