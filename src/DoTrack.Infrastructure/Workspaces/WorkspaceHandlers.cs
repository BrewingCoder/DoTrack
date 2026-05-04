using DoTrack.Application.Workspaces;
using DoTrack.Domain.Workspaces;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.Workspaces;

public sealed class CreateWorkspaceHandler(DoTrackDbContext db, TimeProvider timeProvider) : ICreateWorkspaceHandler
{
    public async Task<CreateWorkspaceResult> HandleAsync(CreateWorkspaceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var slugTaken = await db.Workspaces.AnyAsync(w => w.Slug == command.Slug, cancellationToken);
        if (slugTaken)
        {
            throw new InvalidOperationException($"Workspace slug '{command.Slug}' is already in use.");
        }

        var workspace = new Workspace(WorkspaceId.New(), command.Name, command.Slug, timeProvider.GetUtcNow());
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync(cancellationToken);
        return new CreateWorkspaceResult(workspace.Id);
    }
}

public sealed class ListWorkspacesHandler(DoTrackDbContext db) : IListWorkspacesHandler
{
    public async Task<IReadOnlyList<Workspace>> HandleAsync(CancellationToken cancellationToken)
    {
        var rows = await db.Workspaces.ToListAsync(cancellationToken);
        return rows.OrderBy(w => w.Slug).ToList();
    }
}

public sealed class CreateProjectHandler(DoTrackDbContext db, TimeProvider timeProvider) : ICreateProjectHandler
{
    public async Task<CreateProjectResult> HandleAsync(CreateProjectCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var workspaceExists = await db.Workspaces.AnyAsync(w => w.Id == command.WorkspaceId, cancellationToken);
        if (!workspaceExists)
        {
            throw new InvalidOperationException($"Workspace '{command.WorkspaceId.Value}' not found.");
        }

        var keyTaken = await db.Projects
            .AnyAsync(p => p.WorkspaceId == command.WorkspaceId && p.Key == command.Key, cancellationToken);
        if (keyTaken)
        {
            throw new InvalidOperationException($"Project key '{command.Key}' is already in use in this workspace.");
        }

        var project = new Project(
            ProjectId.New(), command.WorkspaceId, command.Key, command.Name, command.Description,
            timeProvider.GetUtcNow());
        db.Projects.Add(project);
        await db.SaveChangesAsync(cancellationToken);
        return new CreateProjectResult(project.Id);
    }
}

public sealed class ListProjectsHandler(DoTrackDbContext db) : IListProjectsHandler
{
    public async Task<IReadOnlyList<Project>> HandleAsync(ListProjectsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var rows = await db.Projects
            .Where(p => p.WorkspaceId == query.WorkspaceId)
            .ToListAsync(cancellationToken);
        return rows.OrderBy(p => p.Key).ToList();
    }
}
