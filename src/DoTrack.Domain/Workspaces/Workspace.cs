namespace DoTrack.Domain.Workspaces;

public readonly record struct WorkspaceId(Guid Value)
{
    public static WorkspaceId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public sealed class Workspace
{
    public WorkspaceId Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Workspace() { }

    public Workspace(WorkspaceId id, string name, string slug, DateTimeOffset now)
    {
        Id = id;
        Name = name;
        Slug = slug;
        CreatedAt = now;
        UpdatedAt = now;
    }
}
