using DoTrack.Domain.Auditing;

namespace DoTrack.Domain.Identity;

public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

[NotAudited]
public sealed class User
{
    public UserId Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private User() { }

    public User(UserId id, string email, string displayName, DateTimeOffset now)
    {
        Id = id;
        Email = email;
        DisplayName = displayName;
        CreatedAt = now;
        UpdatedAt = now;
    }
}
