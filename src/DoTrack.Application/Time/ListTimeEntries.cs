using DoTrack.Domain.Time;
using DoTrack.Domain.WorkItems;

namespace DoTrack.Application.Time;

public sealed record ListTimeEntriesQuery(WorkItemId WorkItemId);

public interface IListTimeEntriesHandler
{
    Task<IReadOnlyList<TimeEntry>> HandleAsync(ListTimeEntriesQuery query, CancellationToken cancellationToken);
}
