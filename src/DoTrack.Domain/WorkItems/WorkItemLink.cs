using DoTrack.Domain.Identity;

namespace DoTrack.Domain.WorkItems;

public readonly record struct WorkItemLinkId(Guid Value)
{
    public static WorkItemLinkId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public enum WorkItemLinkType
{
    Blocks = 1,
    Duplicates = 2,
    Causes = 3,
    Relates = 4
}

public sealed class WorkItemLink
{
    public WorkItemLinkId Id { get; private set; }
    public WorkItemId SourceId { get; private set; }
    public WorkItemId TargetId { get; private set; }
    public WorkItemLinkType LinkType { get; private set; }
    public UserId? CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private WorkItemLink() { }

    public WorkItemLink(
        WorkItemLinkId id,
        WorkItemId sourceId,
        WorkItemId targetId,
        WorkItemLinkType linkType,
        UserId? createdByUserId,
        DateTimeOffset now)
    {
        if (sourceId == targetId)
        {
            throw new ArgumentException("A work item cannot link to itself.", nameof(targetId));
        }
        Id = id;
        SourceId = sourceId;
        TargetId = targetId;
        LinkType = linkType;
        CreatedByUserId = createdByUserId;
        CreatedAt = now;
    }
}
