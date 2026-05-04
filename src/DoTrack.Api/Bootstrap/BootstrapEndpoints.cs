using DoTrack.Application.Identity;
using DoTrack.Application.Workspaces;
using DoTrack.Domain.Identity;
using DoTrack.Domain.Workspaces;

namespace DoTrack.Api.Bootstrap;

public sealed record CreateWorkspaceRequest(string Name, string Slug);
public sealed record WorkspaceResponse(Guid Id, string Name, string Slug, DateTimeOffset CreatedAt);

public sealed record CreateProjectRequest(string Key, string Name, string? Description);
public sealed record ProjectResponse(Guid Id, Guid WorkspaceId, string Key, string Name, string? Description, int NextWorkItemNumber, DateTimeOffset CreatedAt);

public sealed record CreateUserRequest(string Email, string DisplayName);
public sealed record UserResponse(Guid Id, string Email, string DisplayName, DateTimeOffset CreatedAt);

public static class BootstrapEndpoints
{
    public static IEndpointRouteBuilder MapBootstrapEndpoints(this IEndpointRouteBuilder routes)
    {
        var ws = routes.MapGroup("/api/v1/workspaces").WithTags("Workspaces");
        ws.MapPost("/", CreateWorkspaceAsync);
        ws.MapGet("/", ListWorkspacesAsync).Produces<List<WorkspaceResponse>>(StatusCodes.Status200OK);

        var proj = routes.MapGroup("/api/v1/workspaces/{wsSlug}/projects").WithTags("Projects");
        proj.MapPost("/", CreateProjectAsync);
        proj.MapGet("/", ListProjectsAsync).Produces<List<ProjectResponse>>(StatusCodes.Status200OK);

        var users = routes.MapGroup("/api/v1/users").WithTags("Users");
        users.MapPost("/", CreateUserAsync);
        users.MapGet("/", ListUsersAsync);

        return routes;
    }

    private static async Task<IResult> CreateWorkspaceAsync(
        CreateWorkspaceRequest? body,
        ICreateWorkspaceHandler handler,
        CancellationToken cancellationToken)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Name) || string.IsNullOrWhiteSpace(body.Slug))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["body"] = ["Name and Slug are required."]
            });
        }
        try
        {
            var result = await handler.HandleAsync(new CreateWorkspaceCommand(body.Name, body.Slug), cancellationToken);
            return Results.Created($"/api/v1/workspaces/{body.Slug}", new { id = result.Id.Value, slug = body.Slug });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Slug conflict", detail: ex.Message);
        }
    }

    private static async Task<IResult> ListWorkspacesAsync(
        IListWorkspacesHandler handler,
        CancellationToken cancellationToken)
    {
        var rows = await handler.HandleAsync(cancellationToken);
        return Results.Ok(rows.Select(w => new WorkspaceResponse(w.Id.Value, w.Name, w.Slug, w.CreatedAt)).ToList());
    }

    private static async Task<IResult> CreateProjectAsync(
        string wsSlug,
        CreateProjectRequest? body,
        IProjectResolver projectResolver,
        IListWorkspacesHandler workspaces,
        ICreateProjectHandler handler,
        CancellationToken cancellationToken)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Key) || string.IsNullOrWhiteSpace(body.Name))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["body"] = ["Key and Name are required."]
            });
        }

        // Resolve workspace by slug; the existing IProjectResolver requires a project key,
        // so we list workspaces and find by slug. Cheap for v0 — workspace count is small.
        var wsList = await workspaces.HandleAsync(cancellationToken);
        var workspace = wsList.SingleOrDefault(w => w.Slug == wsSlug);
        if (workspace is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Workspace not found");
        }

        try
        {
            var result = await handler.HandleAsync(
                new CreateProjectCommand(workspace.Id, body.Key, body.Name, body.Description),
                cancellationToken);
            return Results.Created(
                $"/api/v1/workspaces/{wsSlug}/projects/{body.Key}",
                new { id = result.Id.Value, key = body.Key });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Project conflict", detail: ex.Message);
        }
    }

    private static async Task<IResult> ListProjectsAsync(
        string wsSlug,
        IListWorkspacesHandler workspaces,
        IListProjectsHandler handler,
        CancellationToken cancellationToken)
    {
        var wsList = await workspaces.HandleAsync(cancellationToken);
        var workspace = wsList.SingleOrDefault(w => w.Slug == wsSlug);
        if (workspace is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Workspace not found");
        }

        var rows = await handler.HandleAsync(new ListProjectsQuery(workspace.Id), cancellationToken);
        return Results.Ok(rows.Select(p => new ProjectResponse(
            p.Id.Value, p.WorkspaceId.Value, p.Key, p.Name, p.Description, p.NextWorkItemNumber, p.CreatedAt)).ToList());
    }

    private static async Task<IResult> CreateUserAsync(
        CreateUserRequest? body,
        ICreateUserHandler handler,
        CancellationToken cancellationToken)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.DisplayName))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["body"] = ["Email and DisplayName are required."]
            });
        }
        try
        {
            var result = await handler.HandleAsync(new CreateUserCommand(body.Email, body.DisplayName), cancellationToken);
            return Results.Created($"/api/v1/users/{result.Id.Value}",
                new { id = result.Id.Value, email = body.Email });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Email conflict", detail: ex.Message);
        }
    }

    private static async Task<IResult> ListUsersAsync(
        IListUsersHandler handler,
        CancellationToken cancellationToken)
    {
        var rows = await handler.HandleAsync(cancellationToken);
        return Results.Ok(rows.Select(u => new UserResponse(u.Id.Value, u.Email, u.DisplayName, u.CreatedAt)).ToList());
    }
}
