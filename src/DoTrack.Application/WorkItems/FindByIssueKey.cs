using DoTrack.Domain.WorkItems;

namespace DoTrack.Application.WorkItems;

public sealed record FindByIssueKeyQuery(string ProjectKey, int Number);

public interface IFindByIssueKeyHandler
{
    Task<WorkItem?> HandleAsync(FindByIssueKeyQuery query, CancellationToken cancellationToken);
}
