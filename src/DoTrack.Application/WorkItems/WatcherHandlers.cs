using DoTrack.Domain.Identity;
using DoTrack.Domain.WorkItems;

namespace DoTrack.Application.WorkItems;

public sealed record WatchWorkItemCommand(WorkItemId WorkItemId, UserId UserId);
public sealed record UnwatchWorkItemCommand(WorkItemId WorkItemId, UserId UserId);

public interface IWatchWorkItemHandler
{
    Task HandleAsync(WatchWorkItemCommand command, CancellationToken cancellationToken);
}

public interface IUnwatchWorkItemHandler
{
    Task HandleAsync(UnwatchWorkItemCommand command, CancellationToken cancellationToken);
}

public sealed record ListWatchersQuery(WorkItemId WorkItemId);
public interface IListWatchersHandler
{
    Task<IReadOnlyList<UserId>> HandleAsync(ListWatchersQuery query, CancellationToken cancellationToken);
}

public sealed record MyWorkQuery(UserId UserId);
public sealed record MyWorkBucket(string Bucket, IReadOnlyList<WorkItem> Items);
public sealed record MyWorkResult(
    IReadOnlyList<WorkItem> Assigned,
    IReadOnlyList<WorkItem> Reporting,
    IReadOnlyList<WorkItem> Watching);

public interface IMyWorkHandler
{
    Task<MyWorkResult> HandleAsync(MyWorkQuery query, CancellationToken cancellationToken);
}
