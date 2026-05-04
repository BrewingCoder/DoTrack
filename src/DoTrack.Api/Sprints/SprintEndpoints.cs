using DoTrack.Api.WorkItems;
using DoTrack.Application.Sprints;
using DoTrack.Application.WorkItems;
using DoTrack.Application.Workspaces;
using DoTrack.Domain.Sprints;

namespace DoTrack.Api.Sprints;

public static class SprintEndpoints
{
    public static IEndpointRouteBuilder MapSprintEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/workspaces/{wsSlug}/projects/{projKey}/sprints")
            .WithTags("Sprints");

        group.MapPost("/", CreateAsync);
        group.MapGet("/", ListAsync);
        group.MapPatch("/{sprintId:guid}", UpdateAsync);
        group.MapDelete("/{sprintId:guid}", DeleteAsync);
        group.MapGet("/{sprintId:guid}/work-items", ListItemsAsync);

        // Sprint assignment lives next to work items.
        routes.MapPost("/api/v1/workspaces/{wsSlug}/projects/{projKey}/work-items/{number:int}/sprint",
            AssignAsync).WithTags("Sprints");
        routes.MapDelete("/api/v1/workspaces/{wsSlug}/projects/{projKey}/work-items/{number:int}/sprint",
            UnassignAsync).WithTags("Sprints");

        return routes;
    }

    private static async Task<IResult> CreateAsync(
        string wsSlug,
        string projKey,
        CreateSprintRequest? body,
        IProjectResolver projectResolver,
        ICreateSprintHandler createHandler,
        CancellationToken cancellationToken)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Name))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Name is required."] });
        }
        if (body.EndsOn < body.StartsOn)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["endsOn"] = ["EndsOn must be on or after StartsOn."] });
        }

        var project = await projectResolver.ResolveAsync(wsSlug, projKey, cancellationToken);
        if (project is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Project not found");
        }

        var result = await createHandler.HandleAsync(
            new CreateSprintCommand(project.ProjectId, body.Name, body.StartsOn, body.EndsOn),
            cancellationToken);

        var location = $"/api/v1/workspaces/{wsSlug}/projects/{projKey}/sprints/{result.Id.Value}";
        return Results.Created(location, new { id = result.Id.Value });
    }

    private static async Task<IResult> ListAsync(
        string wsSlug,
        string projKey,
        IProjectResolver projectResolver,
        IListSprintsHandler listHandler,
        CancellationToken cancellationToken)
    {
        var project = await projectResolver.ResolveAsync(wsSlug, projKey, cancellationToken);
        if (project is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Project not found");
        }

        var sprints = await listHandler.HandleAsync(new ListSprintsQuery(project.ProjectId), cancellationToken);
        return Results.Ok(sprints.Select(ToResponse).ToList());
    }

    private static async Task<IResult> UpdateAsync(
        string wsSlug,
        string projKey,
        Guid sprintId,
        UpdateSprintRequest? body,
        IProjectResolver projectResolver,
        IUpdateSprintHandler updateHandler,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = ["Body required."] });
        }

        var project = await projectResolver.ResolveAsync(wsSlug, projKey, cancellationToken);
        if (project is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Project not found");
        }

        try
        {
            await updateHandler.HandleAsync(
                new UpdateSprintCommand(new SprintId(sprintId), body.Name, body.StartsOn, body.EndsOn, body.State),
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Sprint not found", detail: ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid update", detail: ex.Message);
        }
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteAsync(
        string wsSlug,
        string projKey,
        Guid sprintId,
        IProjectResolver projectResolver,
        IDeleteSprintHandler deleteHandler,
        CancellationToken cancellationToken)
    {
        var project = await projectResolver.ResolveAsync(wsSlug, projKey, cancellationToken);
        if (project is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Project not found");
        }

        try
        {
            await deleteHandler.HandleAsync(new DeleteSprintCommand(new SprintId(sprintId)), cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Sprint not found", detail: ex.Message);
        }
        return Results.NoContent();
    }

    private static async Task<IResult> ListItemsAsync(
        string wsSlug,
        string projKey,
        Guid sprintId,
        IProjectResolver projectResolver,
        IListSprintWorkItemsHandler listItems,
        CancellationToken cancellationToken)
    {
        var project = await projectResolver.ResolveAsync(wsSlug, projKey, cancellationToken);
        if (project is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Project not found");
        }

        var items = await listItems.HandleAsync(new ListSprintWorkItemsQuery(new SprintId(sprintId)), cancellationToken);
        var responses = items.Select(w => WorkItemContractMapper.ToResponse(w, project.ProjectKey)).ToList();
        return Results.Ok(responses);
    }

    private static async Task<IResult> AssignAsync(
        string wsSlug,
        string projKey,
        int number,
        AssignToSprintRequest? body,
        IProjectResolver projectResolver,
        IGetWorkItemHandler getHandler,
        IAssignToSprintHandler assignHandler,
        CancellationToken cancellationToken)
    {
        if (body is null || body.SprintId == Guid.Empty)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["sprintId"] = ["SprintId is required."] });
        }

        var project = await projectResolver.ResolveAsync(wsSlug, projKey, cancellationToken);
        if (project is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Project not found");
        }

        var workItem = await getHandler.HandleAsync(new GetWorkItemQuery(project.ProjectId, number), cancellationToken);
        if (workItem is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Work item not found");
        }

        try
        {
            await assignHandler.HandleAsync(
                new AssignToSprintCommand(workItem.Id, new SprintId(body.SprintId)),
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Cannot assign to sprint", detail: ex.Message);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> UnassignAsync(
        string wsSlug,
        string projKey,
        int number,
        IProjectResolver projectResolver,
        IGetWorkItemHandler getHandler,
        IAssignToSprintHandler assignHandler,
        CancellationToken cancellationToken)
    {
        var project = await projectResolver.ResolveAsync(wsSlug, projKey, cancellationToken);
        if (project is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Project not found");
        }

        var workItem = await getHandler.HandleAsync(new GetWorkItemQuery(project.ProjectId, number), cancellationToken);
        if (workItem is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Work item not found");
        }

        await assignHandler.HandleAsync(new AssignToSprintCommand(workItem.Id, null), cancellationToken);
        return Results.NoContent();
    }

    private static SprintResponse ToResponse(Sprint s) => new(
        s.Id.Value, s.ProjectId.Value, s.Name, s.StartsOn, s.EndsOn, s.State, s.CreatedAt, s.UpdatedAt);
}
