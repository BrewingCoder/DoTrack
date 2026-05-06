using DoTrack.Domain.Identity;
using DoTrack.Domain.Sprints;
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

public enum WorkItemPriority
{
    ShowStopper = 1,
    Critical = 2,
    Major = 3,
    Normal = 4,
    Minor = 5
}

public sealed class WorkItem
{
    public WorkItemId Id { get; private set; }
    public ProjectId ProjectId { get; private set; }
    public int Number { get; private set; }
    public WorkItemTier Tier { get; private set; }
    public WorkItemType? Type { get; private set; }
    public WorkItemState State { get; private set; } = WorkItemState.Open;
    public WorkItemPriority Priority { get; private set; } = WorkItemPriority.Normal;
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public UserId ReporterId { get; private set; }
    public UserId? AssigneeId { get; private set; }
    public int? EstimatePoints { get; private set; }
    public SprintId? SprintId { get; private set; }
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
        WorkItemPriority priority,
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
        Priority = priority;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public void UpdateTitle(string title, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        }
        Title = title;
        UpdatedAt = now;
    }

    public void UpdateDescription(string? description, DateTimeOffset now)
    {
        Description = description;
        UpdatedAt = now;
    }

    public void Assign(UserId assigneeId, DateTimeOffset now)
    {
        AssigneeId = assigneeId;
        UpdatedAt = now;
    }

    public void Unassign(DateTimeOffset now)
    {
        AssigneeId = null;
        UpdatedAt = now;
    }

    public void SetEstimate(int? points, DateTimeOffset now)
    {
        if (points is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(points), "Estimate must be non-negative.");
        }
        EstimatePoints = points;
        UpdatedAt = now;
    }

    public void TransitionState(WorkItemState newState, DateTimeOffset now)
    {
        // v0: free-form transitions; state-machine rules (legal transitions per type/project)
        // are deferred to v1 when the configurable workflow ships.
        State = newState;
        UpdatedAt = now;
    }

    public void SetPriority(WorkItemPriority priority, DateTimeOffset now)
    {
        if (!Enum.IsDefined(priority))
        {
            throw new ArgumentOutOfRangeException(nameof(priority), priority, "Unknown priority value.");
        }
        Priority = priority;
        UpdatedAt = now;
    }

    public void AssignToSprint(SprintId sprintId, DateTimeOffset now)
    {
        if (Tier != WorkItemTier.Item)
        {
            throw new InvalidOperationException("Only Items can be assigned to a sprint; Epics and Features are scope, not sprint work.");
        }
        SprintId = sprintId;
        UpdatedAt = now;
    }

    public void RemoveFromSprint(DateTimeOffset now)
    {
        SprintId = null;
        UpdatedAt = now;
    }
}
