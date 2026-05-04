using DoTrack.Application.Comments;
using DoTrack.Domain.Comments;
using DoTrack.Infrastructure.Outbox;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.Comments;

public sealed class AddCommentHandler(DoTrackDbContext db, TimeProvider timeProvider, OutboxEmitter outbox) : IAddCommentHandler
{
    public async Task<AddCommentResult> HandleAsync(AddCommentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var workItem = await db.WorkItems.SingleOrDefaultAsync(w => w.Id == command.WorkItemId, cancellationToken)
            ?? throw new InvalidOperationException($"WorkItem '{command.WorkItemId.Value}' not found.");

        var now = timeProvider.GetUtcNow();
        var comment = new Comment(
            CommentId.New(),
            command.WorkItemId,
            command.AuthorId,
            command.Body,
            command.IsInternal,
            now);

        db.Comments.Add(comment);

        var project = await db.Projects.SingleAsync(p => p.Id == workItem.ProjectId, cancellationToken);
        // Internal comments don't go to automation by default — same posture as
        // the visibility flag at the UI level. n8n can still route them with
        // a more permissive subscription if needed.
        if (!command.IsInternal)
        {
            outbox.Emit("issue.commented", project.Key, new
            {
                workItemId = workItem.Id.Value,
                commentId = comment.Id.Value,
                projectKey = project.Key,
                number = workItem.Number,
                key = $"{project.Key}-{workItem.Number}",
                authorId = comment.AuthorId.Value,
                body = comment.Body,
                isInternal = comment.IsInternal,
                createdAt = comment.CreatedAt
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return new AddCommentResult(comment.Id);
    }
}
