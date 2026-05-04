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
        group.MapGet("/{number:int}", GetAsync);

        return routes;
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
}
