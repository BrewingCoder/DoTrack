using DoTrack.Domain.WorkItems;

namespace DoTrack.Application.WorkItems;

public sealed record SetWorkItemParentCommand(WorkItemId WorkItemId, WorkItemId? ParentId);

public interface ISetWorkItemParentHandler
{
    Task HandleAsync(SetWorkItemParentCommand command, CancellationToken cancellationToken);
}
