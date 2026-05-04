using DoTrack.Application.Comments;
using DoTrack.Domain.Comments;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.Comments;

public sealed class AddCommentHandler(DoTrackDbContext db, TimeProvider timeProvider) : IAddCommentHandler
{
    public async Task<AddCommentResult> HandleAsync(AddCommentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var workItemExists = await db.WorkItems.AnyAsync(w => w.Id == command.WorkItemId, cancellationToken);
        if (!workItemExists)
        {
            throw new InvalidOperationException($"WorkItem '{command.WorkItemId.Value}' not found.");
        }

        var comment = new Comment(
            CommentId.New(),
            command.WorkItemId,
            command.AuthorId,
            command.Body,
            command.IsInternal,
            timeProvider.GetUtcNow());

        db.Comments.Add(comment);
        await db.SaveChangesAsync(cancellationToken);

        return new AddCommentResult(comment.Id);
    }
}
