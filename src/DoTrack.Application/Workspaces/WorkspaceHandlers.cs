using DoTrack.Domain.Workspaces;

namespace DoTrack.Application.Workspaces;

public sealed record CreateWorkspaceCommand(string Name, string Slug);
public sealed record CreateWorkspaceResult(WorkspaceId Id);
public interface ICreateWorkspaceHandler
{
    Task<CreateWorkspaceResult> HandleAsync(CreateWorkspaceCommand command, CancellationToken cancellationToken);
}

public interface IListWorkspacesHandler
{
    Task<IReadOnlyList<Workspace>> HandleAsync(CancellationToken cancellationToken);
}

public sealed record CreateProjectCommand(WorkspaceId WorkspaceId, string Key, string Name, string? Description);
public sealed record CreateProjectResult(ProjectId Id);
public interface ICreateProjectHandler
{
    Task<CreateProjectResult> HandleAsync(CreateProjectCommand command, CancellationToken cancellationToken);
}

public sealed record ListProjectsQuery(WorkspaceId WorkspaceId);
public interface IListProjectsHandler
{
    Task<IReadOnlyList<Project>> HandleAsync(ListProjectsQuery query, CancellationToken cancellationToken);
}
