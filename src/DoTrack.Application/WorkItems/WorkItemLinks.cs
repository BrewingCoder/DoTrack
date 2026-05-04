using DoTrack.Domain.Identity;
using DoTrack.Domain.WorkItems;

namespace DoTrack.Application.WorkItems;

public sealed record AddWorkItemLinkCommand(
    WorkItemId SourceId,
    WorkItemId TargetId,
    WorkItemLinkType LinkType,
    UserId? CreatedByUserId);
public sealed record AddWorkItemLinkResult(WorkItemLinkId Id);
public interface IAddWorkItemLinkHandler
{
    Task<AddWorkItemLinkResult> HandleAsync(AddWorkItemLinkCommand command, CancellationToken cancellationToken);
}

public sealed record RemoveWorkItemLinkCommand(WorkItemLinkId LinkId);
public interface IRemoveWorkItemLinkHandler
{
    Task HandleAsync(RemoveWorkItemLinkCommand command, CancellationToken cancellationToken);
}

public sealed record ListWorkItemLinksQuery(WorkItemId WorkItemId);

public sealed record WorkItemLinkView(
    WorkItemLinkId Id,
    WorkItemId OtherWorkItemId,
    WorkItemLinkType LinkType,
    bool IsOutbound,
    DateTimeOffset CreatedAt);

public interface IListWorkItemLinksHandler
{
    Task<IReadOnlyList<WorkItemLinkView>> HandleAsync(ListWorkItemLinksQuery query, CancellationToken cancellationToken);
}
