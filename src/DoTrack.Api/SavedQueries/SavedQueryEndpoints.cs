using DoTrack.Application.SavedQueries;
using DoTrack.Application.Workspaces;
using DoTrack.Domain.Identity;
using DoTrack.Domain.SavedQueries;
using DoTrack.Domain.Workspaces;

namespace DoTrack.Api.SavedQueries;

public sealed record CreateSavedQueryRequest(
    Guid OwnerUserId,
    SavedQueryScope Scope,
    string? WorkspaceSlug,
    string? ProjectKey,
    string Name,
    string QueryText,
    string? Color,
    string? Icon);

public sealed record UpdateSavedQueryRequest(
    string? Name, string? QueryText, string? Color, string? Icon);

public sealed record SavedQueryResponse(
    Guid Id,
    Guid OwnerUserId,
    SavedQueryScope Scope,
    Guid? ProjectId,
    string Name,
    string QueryText,
    string? Color,
    string? Icon,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public static class SavedQueryEndpoints
{
    public static IEndpointRouteBuilder MapSavedQueryEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/saved-queries").WithTags("SavedQueries");
        group.MapPost("/", CreateAsync);
        group.MapGet("/", ListAsync);
        group.MapPatch("/{queryId:guid}", UpdateAsync);
        group.MapDelete("/{queryId:guid}", DeleteAsync);
        return routes;
    }

    private static async Task<IResult> CreateAsync(
        CreateSavedQueryRequest? body,
        IProjectResolver projectResolver,
        ICreateSavedQueryHandler handler,
        CancellationToken cancellationToken)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Name) || body.QueryText is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["body"] = ["Name and QueryText required."] });
        }
        if (body.OwnerUserId == Guid.Empty)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["ownerUserId"] = ["Required."] });
        }

        ProjectId? projectId = null;
        if (body.Scope == SavedQueryScope.Project)
        {
            if (string.IsNullOrEmpty(body.WorkspaceSlug) || string.IsNullOrEmpty(body.ProjectKey))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["projectKey"] = ["Project scope requires WorkspaceSlug and ProjectKey."]
                });
            }
            var scope = await projectResolver.ResolveAsync(body.WorkspaceSlug, body.ProjectKey, cancellationToken);
            if (scope is null)
            {
                return Results.Problem(statusCode: 404, title: "Project not found");
            }
            projectId = scope.ProjectId;
        }

        try
        {
            var result = await handler.HandleAsync(
                new CreateSavedQueryCommand(
                    new UserId(body.OwnerUserId), body.Scope, projectId,
                    body.Name, body.QueryText, body.Color, body.Icon),
                cancellationToken);
            return Results.Created($"/api/v1/saved-queries/{result.Id.Value}", new { id = result.Id.Value });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(statusCode: 404, title: "Reference not found", detail: ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(statusCode: 400, title: "Invalid request", detail: ex.Message);
        }
    }

    private static async Task<IResult> ListAsync(
        Guid? userId,
        string? workspaceSlug,
        string? projectKey,
        bool? includePublic,
        IProjectResolver projectResolver,
        IListSavedQueriesHandler handler,
        CancellationToken cancellationToken)
    {
        ProjectId? projectId = null;
        if (!string.IsNullOrEmpty(workspaceSlug) && !string.IsNullOrEmpty(projectKey))
        {
            var scope = await projectResolver.ResolveAsync(workspaceSlug, projectKey, cancellationToken);
            if (scope is null)
            {
                return Results.Problem(statusCode: 404, title: "Project not found");
            }
            projectId = scope.ProjectId;
        }

        var rows = await handler.HandleAsync(
            new ListSavedQueriesQuery(
                userId is { } u ? new UserId(u) : null,
                projectId,
                includePublic ?? true),
            cancellationToken);

        return Results.Ok(rows.Select(s => new SavedQueryResponse(
            s.Id.Value, s.OwnerUserId.Value, s.Scope, s.ProjectId?.Value,
            s.Name, s.QueryText, s.Color, s.Icon, s.CreatedAt, s.UpdatedAt)).ToList());
    }

    private static async Task<IResult> UpdateAsync(
        Guid queryId,
        UpdateSavedQueryRequest? body,
        IUpdateSavedQueryHandler handler,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["body"] = ["Body required."] });
        }
        try
        {
            await handler.HandleAsync(
                new UpdateSavedQueryCommand(new SavedQueryId(queryId), body.Name, body.QueryText, body.Color, body.Icon),
                cancellationToken);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(statusCode: 404, title: "Saved query not found", detail: ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(statusCode: 400, title: "Invalid update", detail: ex.Message);
        }
    }

    private static async Task<IResult> DeleteAsync(
        Guid queryId,
        IDeleteSavedQueryHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeleteSavedQueryCommand(new SavedQueryId(queryId)), cancellationToken);
        return Results.NoContent();
    }
}
