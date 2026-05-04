namespace DoTrack.Api.Comments;

public sealed record AddCommentRequest(
    Guid AuthorId,
    string Body,
    bool IsInternal);

public sealed record CommentResponse(
    Guid Id,
    Guid WorkItemId,
    Guid AuthorId,
    string Body,
    bool IsInternal,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
