using DoTrack.Domain.Workspaces;

namespace DoTrack.Application.Workspaces;

public sealed record ProjectScope(WorkspaceId WorkspaceId, ProjectId ProjectId, string ProjectKey, string WorkspaceSlug);

public interface IProjectResolver
{
    Task<ProjectScope?> ResolveAsync(string workspaceSlug, string projectKey, CancellationToken cancellationToken);
}
