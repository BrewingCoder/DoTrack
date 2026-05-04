using DoTrack.Domain.WorkItems;

namespace DoTrack.Domain.Milestones;

public readonly record struct MilestoneId(Guid Value)
{
    public static MilestoneId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public enum MilestoneState
{
    Active = 1,
    Completed = 2,
    Cancelled = 3
}

public sealed class Milestone
{
    public MilestoneId Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateOnly? TargetDate { get; private set; }
    public decimal? HoursBudget { get; private set; }
    public bool VisibleToClient { get; private set; }
    public MilestoneState State { get; private set; } = MilestoneState.Active;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Milestone() { }

    public Milestone(
        MilestoneId id,
        string name,
        string? description,
        DateOnly? targetDate,
        decimal? hoursBudget,
        bool visibleToClient,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }
        if (hoursBudget is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hoursBudget), "HoursBudget must be non-negative.");
        }
        Id = id;
        Name = name;
        Description = description;
        TargetDate = targetDate;
        HoursBudget = hoursBudget;
        VisibleToClient = visibleToClient;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public void Update(string? name, string? description, DateOnly? targetDate, decimal? hoursBudget, bool? visibleToClient, DateTimeOffset now)
    {
        if (name is not null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name cannot be blank.", nameof(name));
            }
            Name = name;
        }
        if (description is not null)
        {
            Description = description;
        }
        if (targetDate is not null)
        {
            TargetDate = targetDate;
        }
        if (hoursBudget is not null)
        {
            if (hoursBudget < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hoursBudget));
            }
            HoursBudget = hoursBudget;
        }
        if (visibleToClient is not null)
        {
            VisibleToClient = visibleToClient.Value;
        }
        UpdatedAt = now;
    }

    public void Complete(DateTimeOffset now)
    {
        State = MilestoneState.Completed;
        UpdatedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        State = MilestoneState.Cancelled;
        UpdatedAt = now;
    }
}

public sealed class MilestoneScope
{
    public MilestoneId MilestoneId { get; private set; }
    public WorkItemId WorkItemId { get; private set; }
    public DateTimeOffset AddedAt { get; private set; }

    private MilestoneScope() { }

    public MilestoneScope(MilestoneId milestoneId, WorkItemId workItemId, DateTimeOffset now)
    {
        MilestoneId = milestoneId;
        WorkItemId = workItemId;
        AddedAt = now;
    }
}
