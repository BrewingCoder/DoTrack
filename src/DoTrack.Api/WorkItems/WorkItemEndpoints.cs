using DoTrack.Application.WorkItems;
using DoTrack.Application.Workspaces;

namespace DoTrack.Api.WorkItems;

public static class WorkItemEndpoints
{
    public static IEndpointRouteBuilder MapWorkItemEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/workspaces/{wsSlug}/projects/{projKey}/work-items")
            .WithTags("WorkItems");

        group.MapPost("/", CreateAsync);
        group.MapGet("/", ListAsync).Produces<List<WorkItemResponse>>(StatusCodes.Status200OK);
        group.MapGet("/{number:int}", GetAsync).Produces<WorkItemResponse>(StatusCodes.Status200OK);
        group.MapPatch("/{number:int}", PatchAsync);
        group.MapPost("/{number:int}/parent", SetParentAsync);
        group.MapDelete("/{number:int}/parent", RemoveParentAsync);

        return routes;
    }

    private static async Task<IResult> ListAsync(
        string wsSlug,
        string projKey,
        IProjectResolver projectResolver,
        IListWorkItemsForProjectHandler listHandler,
        CancellationToken cancellationToken)
    {
        var project = await projectResolver.ResolveAsync(wsSlug, projKey, cancellationToken);
        if (project is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Project not found",
                detail: $"No project '{projKey}' in workspace '{wsSlug}'.");
        }

        var items = await listHandler.HandleAsync(new ListWorkItemsForProjectQuery(project.ProjectId), cancellationToken);
        var responses = items.Select(w => WorkItemContractMapper.ToResponse(w, project.ProjectKey)).ToList();
        return Results.Ok(responses);
    }

    private static async Task<IResult> CreateAsync(
        string wsSlug,
        string projKey,
        CreateWorkItemRequest? body,
        IProjectResolver projectResolver,
        ICreateWorkItemHandler createHandler,
        IGetWorkItemHandler getHandler,
        CancellationToken cancellationToken)
    {
        var validationErrors = CreateWorkItemRequestValidator.Validate(body);
        if (validationErrors is not null)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var project = await projectResolver.ResolveAsync(wsSlug, projKey, cancellationToken);
        if (project is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Project not found",
                detail: $"No project '{projKey}' in workspace '{wsSlug}'.");
        }

        var command = WorkItemContractMapper.ToCommand(body!, project.ProjectId);
        var result = await createHandler.HandleAsync(command, cancellationToken);

        var created = await getHandler.HandleAsync(
            new GetWorkItemQuery(project.ProjectId, result.Number),
            cancellationToken);
        if (created is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Created work item could not be re-read.");
        }

        var response = WorkItemContractMapper.ToResponse(created, project.ProjectKey);
        var location = $"/api/v1/workspaces/{wsSlug}/projects/{projKey}/work-items/{result.Number}";
        return Results.Created(location, response);
    }

    private static async Task<IResult> GetAsync(
        string wsSlug,
        string projKey,
        int number,
        IProjectResolver projectResolver,
        IGetWorkItemHandler getHandler,
        CancellationToken cancellationToken)
    {
        var project = await projectResolver.ResolveAsync(wsSlug, projKey, cancellationToken);
        if (project is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Project not found",
                detail: $"No project '{projKey}' in workspace '{wsSlug}'.");
        }

        var workItem = await getHandler.HandleAsync(
            new GetWorkItemQuery(project.ProjectId, number),
            cancellationToken);
        if (workItem is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Work item not found",
                detail: $"{projKey}-{number} does not exist.");
        }

        return Results.Ok(WorkItemContractMapper.ToResponse(workItem, project.ProjectKey));
    }

    private static async Task<IResult> PatchAsync(
        string wsSlug,
        string projKey,
        int number,
        UpdateWorkItemRequest? body,
        IProjectResolver projectResolver,
        IGetWorkItemHandler getHandler,
        IUpdateWorkItemHandler updateHandler,
        CancellationToken cancellationToken)
    {
        var validationErrors = UpdateWorkItemRequestValidator.Validate(body);
        if (validationErrors is not null)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var project = await projectResolver.ResolveAsync(wsSlug, projKey, cancellationToken);
        if (project is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Project not found",
                detail: $"No project '{projKey}' in workspace '{wsSlug}'.");
        }

        var existing = await getHandler.HandleAsync(
            new GetWorkItemQuery(project.ProjectId, number),
            cancellationToken);
        if (existing is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Work item not found",
                detail: $"{projKey}-{number} does not exist.");
        }

        var command = WorkItemContractMapper.ToCommand(body!, existing.Id);
        await updateHandler.HandleAsync(command, cancellationToken);

        var updated = await getHandler.HandleAsync(
            new GetWorkItemQuery(project.ProjectId, number),
            cancellationToken);
        return Results.Ok(WorkItemContractMapper.ToResponse(updated!, project.ProjectKey));
    }

    private static async Task<IResult> SetParentAsync(
        string wsSlug,
        string projKey,
        int number,
        SetParentRequest? body,
        IProjectResolver projectResolver,
        IGetWorkItemHandler getHandler,
        ISetWorkItemParentHandler setParentHandler,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["Request body is required."]
            });
        }

        var project = await projectResolver.ResolveAsync(wsSlug, projKey, cancellationToken);
        if (project is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Project not found");
        }

        var child = await getHandler.HandleAsync(
            new GetWorkItemQuery(project.ProjectId, number),
            cancellationToken);
        if (child is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Work item not found",
                detail: $"{projKey}-{number} does not exist.");
        }

        var parentWsSlug = body.ParentWorkspaceSlug ?? wsSlug;
        var parentProjKey = body.ParentProjectKey ?? projKey;
        var parentScope = await projectResolver.ResolveAsync(parentWsSlug, parentProjKey, cancellationToken);
        if (parentScope is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Parent project not found");
        }

        var parent = await getHandler.HandleAsync(
            new GetWorkItemQuery(parentScope.ProjectId, body.ParentNumber),
            cancellationToken);
        if (parent is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Parent work item not found",
                detail: $"{parentProjKey}-{body.ParentNumber} does not exist.");
        }

        try
        {
            await setParentHandler.HandleAsync(
                new SetWorkItemParentCommand(child.Id, parent.Id),
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid parent link",
                detail: ex.Message);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> RemoveParentAsync(
        string wsSlug,
        string projKey,
        int number,
        IProjectResolver projectResolver,
        IGetWorkItemHandler getHandler,
        ISetWorkItemParentHandler setParentHandler,
        CancellationToken cancellationToken)
    {
        var project = await projectResolver.ResolveAsync(wsSlug, projKey, cancellationToken);
        if (project is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Project not found");
        }

        var child = await getHandler.HandleAsync(
            new GetWorkItemQuery(project.ProjectId, number),
            cancellationToken);
        if (child is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Work item not found");
        }

        await setParentHandler.HandleAsync(
            new SetWorkItemParentCommand(child.Id, null),
            cancellationToken);
        return Results.NoContent();
    }
}
