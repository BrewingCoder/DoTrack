using DoTrack.Domain.Identity;
using DoTrack.Domain.Workspaces;

namespace DoTrack.Domain.SavedQueries;

public readonly record struct SavedQueryId(Guid Value)
{
    public static SavedQueryId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public enum SavedQueryScope
{
    Personal = 1,
    Project = 2,
    Public = 3
}

public sealed class SavedQuery
{
    public SavedQueryId Id { get; private set; }
    public UserId OwnerUserId { get; private set; }
    public SavedQueryScope Scope { get; private set; }
    public ProjectId? ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string QueryText { get; private set; } = string.Empty;
    public string? Color { get; private set; }
    public string? Icon { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private SavedQuery() { }

    public SavedQuery(
        SavedQueryId id,
        UserId ownerUserId,
        SavedQueryScope scope,
        ProjectId? projectId,
        string name,
        string queryText,
        string? color,
        string? icon,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }
        ArgumentNullException.ThrowIfNull(queryText);
        if (scope == SavedQueryScope.Project && projectId is null)
        {
            throw new ArgumentException("Project scope requires a ProjectId.", nameof(projectId));
        }
        if (scope != SavedQueryScope.Project && projectId is not null)
        {
            throw new ArgumentException("Non-project scope cannot have a ProjectId.", nameof(projectId));
        }

        Id = id;
        OwnerUserId = ownerUserId;
        Scope = scope;
        ProjectId = projectId;
        Name = name;
        QueryText = queryText;
        Color = color;
        Icon = icon;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public void Update(string? name, string? queryText, string? color, string? icon, DateTimeOffset now)
    {
        if (name is not null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name cannot be blank.", nameof(name));
            }
            Name = name;
        }
        if (queryText is not null)
        {
            QueryText = queryText;
        }
        if (color is not null)
        {
            Color = color;
        }
        if (icon is not null)
        {
            Icon = icon;
        }
        UpdatedAt = now;
    }
}
