using DoTrack.Domain.Identity;
using DoTrack.Domain.WorkItems;

namespace DoTrack.Domain.Comments;

public readonly record struct CommentId(Guid Value)
{
    public static CommentId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public sealed class Comment
{
    public CommentId Id { get; private set; }
    public WorkItemId WorkItemId { get; private set; }
    public UserId AuthorId { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public bool IsInternal { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private Comment() { }

    public Comment(
        CommentId id,
        WorkItemId workItemId,
        UserId authorId,
        string body,
        bool isInternal,
        DateTimeOffset now)
    {
        Id = id;
        WorkItemId = workItemId;
        AuthorId = authorId;
        Body = body;
        IsInternal = isInternal;
        CreatedAt = now;
    }
}
