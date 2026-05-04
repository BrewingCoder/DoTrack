using DoTrack.Domain.Sprints;
using DoTrack.Domain.WorkItems;
using DoTrack.Domain.Workspaces;

namespace DoTrack.Application.Sprints;

public sealed record CreateSprintCommand(ProjectId ProjectId, string Name, DateOnly StartsOn, DateOnly EndsOn);
public sealed record CreateSprintResult(SprintId Id);
public interface ICreateSprintHandler
{
    Task<CreateSprintResult> HandleAsync(CreateSprintCommand command, CancellationToken cancellationToken);
}

public sealed record UpdateSprintCommand(
    SprintId SprintId,
    string? Name,
    DateOnly? StartsOn,
    DateOnly? EndsOn,
    SprintState? State);
public interface IUpdateSprintHandler
{
    Task HandleAsync(UpdateSprintCommand command, CancellationToken cancellationToken);
}

public sealed record DeleteSprintCommand(SprintId SprintId);
public interface IDeleteSprintHandler
{
    Task HandleAsync(DeleteSprintCommand command, CancellationToken cancellationToken);
}

public sealed record ListSprintsQuery(ProjectId ProjectId);
public interface IListSprintsHandler
{
    Task<IReadOnlyList<Sprint>> HandleAsync(ListSprintsQuery query, CancellationToken cancellationToken);
}

public sealed record AssignToSprintCommand(WorkItemId WorkItemId, SprintId? SprintId);
public interface IAssignToSprintHandler
{
    Task HandleAsync(AssignToSprintCommand command, CancellationToken cancellationToken);
}

public sealed record ListSprintWorkItemsQuery(SprintId SprintId);
public interface IListSprintWorkItemsHandler
{
    Task<IReadOnlyList<WorkItem>> HandleAsync(ListSprintWorkItemsQuery query, CancellationToken cancellationToken);
}
