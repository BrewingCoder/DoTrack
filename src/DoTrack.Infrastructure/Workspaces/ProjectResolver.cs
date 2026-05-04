using DoTrack.Application.Workspaces;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.Workspaces;

public sealed class ProjectResolver(DoTrackDbContext db) : IProjectResolver
{
    public async Task<ProjectScope?> ResolveAsync(
        string workspaceSlug,
        string projectKey,
        CancellationToken cancellationToken)
    {
        return await (
            from ws in db.Workspaces
            join p in db.Projects on ws.Id equals p.WorkspaceId
            where ws.Slug == workspaceSlug && p.Key == projectKey
            select new ProjectScope(ws.Id, p.Id, p.Key, ws.Slug)
        ).SingleOrDefaultAsync(cancellationToken);
    }
}
