using DoTrack.Domain.Identity;
using DoTrack.Domain.Workspaces;

namespace DoTrack.Domain.WorkItems;

public readonly record struct WorkItemId(Guid Value)
{
    public static WorkItemId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public enum WorkItemTier
{
    Epic = 1,
    Feature = 2,
    Item = 3
}

public enum WorkItemType
{
    Story = 1,
    Bug = 2,
    Task = 3,
    Spike = 4,
    Chore = 5
}

public enum WorkItemState
{
    Open = 1,
    InProgress = 2,
    AwaitingClientReview = 3,
    Accepted = 4
}

public sealed class WorkItem
{
    public WorkItemId Id { get; private set; }
    public ProjectId ProjectId { get; private set; }
    public int Number { get; private set; }
    public WorkItemTier Tier { get; private set; }
    public WorkItemType? Type { get; private set; }
    public WorkItemState State { get; private set; } = WorkItemState.Open;
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public UserId ReporterId { get; private set; }
    public UserId? AssigneeId { get; private set; }
    public int? EstimatePoints { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private WorkItem() { }

    public WorkItem(
        WorkItemId id,
        ProjectId projectId,
        int number,
        WorkItemTier tier,
        WorkItemType? type,
        string title,
        string? description,
        UserId reporterId,
        UserId? assigneeId,
        int? estimatePoints,
        DateTimeOffset now)
    {
        Id = id;
        ProjectId = projectId;
        Number = number;
        Tier = tier;
        Type = type;
        Title = title;
        Description = description;
        ReporterId = reporterId;
        AssigneeId = assigneeId;
        EstimatePoints = estimatePoints;
        CreatedAt = now;
        UpdatedAt = now;
    }
}
