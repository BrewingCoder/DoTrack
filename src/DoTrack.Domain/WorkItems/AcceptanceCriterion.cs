using DoTrack.Domain.Identity;

namespace DoTrack.Domain.WorkItems;

public readonly record struct AcceptanceCriterionId(Guid Value)
{
    public static AcceptanceCriterionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public enum AcceptanceCriterionStatus
{
    Pending = 1,
    Met = 2,
    Waived = 3
}

public sealed class AcceptanceCriterion
{
    public AcceptanceCriterionId Id { get; private set; }
    public WorkItemId WorkItemId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public AcceptanceCriterionStatus Status { get; private set; } = AcceptanceCriterionStatus.Pending;
    public UserId? CheckedByUserId { get; private set; }
    public DateTimeOffset? CheckedAt { get; private set; }
    public string? Comment { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private AcceptanceCriterion() { }

    public AcceptanceCriterion(
        AcceptanceCriterionId id,
        WorkItemId workItemId,
        string description,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }
        Id = id;
        WorkItemId = workItemId;
        Description = description;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public void MarkMet(UserId userId, DateTimeOffset now, string? comment = null)
    {
        Status = AcceptanceCriterionStatus.Met;
        CheckedByUserId = userId;
        CheckedAt = now;
        Comment = comment;
        UpdatedAt = now;
    }

    public void Waive(UserId userId, DateTimeOffset now, string comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            throw new ArgumentException("Waiving a criterion requires a reason.", nameof(comment));
        }
        Status = AcceptanceCriterionStatus.Waived;
        CheckedByUserId = userId;
        CheckedAt = now;
        Comment = comment;
        UpdatedAt = now;
    }

    public void ResetToPending(DateTimeOffset now)
    {
        Status = AcceptanceCriterionStatus.Pending;
        CheckedByUserId = null;
        CheckedAt = null;
        Comment = null;
        UpdatedAt = now;
    }

    public void UpdateDescription(string description, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }
        Description = description;
        UpdatedAt = now;
    }
}
