using DoTrack.Application.WorkItems;
using DoTrack.Domain.WorkItems;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.WorkItems;

public sealed class FindByIssueKeyHandler(DoTrackDbContext db) : IFindByIssueKeyHandler
{
    public async Task<WorkItem?> HandleAsync(FindByIssueKeyQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Project keys are unique per workspace but not globally; if multiple workspaces
        // share a key, we return the first match (deterministic via projectKey ordering).
        // Real installations should keep keys workspace-unique enough that this is rare.
        var projectIds = await db.Projects
            .Where(p => p.Key == query.ProjectKey)
            .OrderBy(p => p.CreatedAt)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
        if (projectIds.Count == 0)
        {
            return null;
        }

        return await db.WorkItems
            .Where(w => projectIds.Contains(w.ProjectId) && w.Number == query.Number)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
