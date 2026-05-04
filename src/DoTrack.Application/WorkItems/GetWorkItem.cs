using DoTrack.Domain.WorkItems;
using DoTrack.Domain.Workspaces;

namespace DoTrack.Application.WorkItems;

public sealed record GetWorkItemQuery(ProjectId ProjectId, int Number);

public interface IGetWorkItemHandler
{
    Task<WorkItem?> HandleAsync(GetWorkItemQuery query, CancellationToken cancellationToken);
}
