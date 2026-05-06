using DoTrack.Domain.WorkItems;
using DoTrack.Domain.Workspaces;

namespace DoTrack.Application.WorkItems;

public sealed record GetWorkItemQuery(ProjectId ProjectId, int Number);

public interface IGetWorkItemHandler
{
    Task<WorkItem?> HandleAsync(GetWorkItemQuery query, CancellationToken cancellationToken);
}

public sealed record ListWorkItemsForProjectQuery(ProjectId ProjectId);

// ParentKey carries the full project-prefixed key (e.g. "PROJ-3") of the
// direct parent — null when the work item has no parent. Cross-project
// parents (Epic→Feature spanning projects) include the other project's key.
public sealed record ListedWorkItem(WorkItem Item, string? ParentKey);

public interface IListWorkItemsForProjectHandler
{
    Task<IReadOnlyList<ListedWorkItem>> HandleAsync(ListWorkItemsForProjectQuery query, CancellationToken cancellationToken);
}
