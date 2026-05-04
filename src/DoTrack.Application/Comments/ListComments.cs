using DoTrack.Domain.Comments;
using DoTrack.Domain.WorkItems;

namespace DoTrack.Application.Comments;

public sealed record ListCommentsQuery(WorkItemId WorkItemId, bool IncludeInternal);

public interface IListCommentsHandler
{
    Task<IReadOnlyList<Comment>> HandleAsync(ListCommentsQuery query, CancellationToken cancellationToken);
}
