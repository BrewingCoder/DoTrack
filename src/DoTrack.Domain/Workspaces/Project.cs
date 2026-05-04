using DoTrack.Domain.Auditing;

namespace DoTrack.Domain.Workspaces;

public readonly record struct ProjectId(Guid Value)
{
    public static ProjectId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public sealed class Project
{
    public ProjectId Id { get; private set; }
    public WorkspaceId WorkspaceId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    // Internal allocation counter; not user-meaningful and not auditable.
    // Filtered out of audit field_changes via [NotAudited] so sequence
    // bumps never produce audit-spam rows.
    [NotAudited]
    public int NextWorkItemNumber { get; private set; } = 1;

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Project() { }

    public Project(
        ProjectId id,
        WorkspaceId workspaceId,
        string key,
        string name,
        string? description,
        DateTimeOffset now)
    {
        Id = id;
        WorkspaceId = workspaceId;
        Key = key;
        Name = name;
        Description = description;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public int AllocateNextWorkItemNumber()
    {
        var allocated = NextWorkItemNumber;
        NextWorkItemNumber = allocated + 1;
        return allocated;
    }
}
