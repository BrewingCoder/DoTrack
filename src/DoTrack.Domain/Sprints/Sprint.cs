using DoTrack.Domain.Workspaces;

namespace DoTrack.Domain.Sprints;

public readonly record struct SprintId(Guid Value)
{
    public static SprintId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public enum SprintState
{
    Planning = 1,
    Active = 2,
    Completed = 3
}

public sealed class Sprint
{
    public SprintId Id { get; private set; }
    public ProjectId ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateOnly StartsOn { get; private set; }
    public DateOnly EndsOn { get; private set; }
    public SprintState State { get; private set; } = SprintState.Planning;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Sprint() { }

    public Sprint(SprintId id, ProjectId projectId, string name, DateOnly startsOn, DateOnly endsOn, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }
        if (endsOn < startsOn)
        {
            throw new ArgumentException("EndsOn must be on or after StartsOn.", nameof(endsOn));
        }
        Id = id;
        ProjectId = projectId;
        Name = name;
        StartsOn = startsOn;
        EndsOn = endsOn;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public void Activate(DateTimeOffset now)
    {
        State = SprintState.Active;
        UpdatedAt = now;
    }

    public void Complete(DateTimeOffset now)
    {
        State = SprintState.Completed;
        UpdatedAt = now;
    }

    public void Reschedule(DateOnly startsOn, DateOnly endsOn, DateTimeOffset now)
    {
        if (endsOn < startsOn)
        {
            throw new ArgumentException("EndsOn must be on or after StartsOn.", nameof(endsOn));
        }
        StartsOn = startsOn;
        EndsOn = endsOn;
        UpdatedAt = now;
    }

    public void Rename(string name, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }
        Name = name;
        UpdatedAt = now;
    }
}
