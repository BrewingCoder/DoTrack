using DoTrack.Domain.Comments;
using DoTrack.Domain.Identity;
using DoTrack.Domain.WorkItems;

namespace DoTrack.Application.Comments;

public sealed record AddCommentCommand(
    WorkItemId WorkItemId,
    UserId AuthorId,
    string Body,
    bool IsInternal);

public sealed record AddCommentResult(CommentId Id);

public interface IAddCommentHandler
{
    Task<AddCommentResult> HandleAsync(AddCommentCommand command, CancellationToken cancellationToken);
}
