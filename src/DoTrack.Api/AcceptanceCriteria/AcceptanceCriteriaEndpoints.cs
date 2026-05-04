using DoTrack.Application.AcceptanceCriteria;
using DoTrack.Application.WorkItems;
using DoTrack.Application.Workspaces;
using DoTrack.Domain.Identity;
using DoTrack.Domain.WorkItems;

namespace DoTrack.Api.AcceptanceCriteria;

public static class AcceptanceCriteriaEndpoints
{
    public static IEndpointRouteBuilder MapAcceptanceCriteriaEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/workspaces/{wsSlug}/projects/{projKey}/work-items/{number:int}/acceptance-criteria")
            .WithTags("AcceptanceCriteria");

        group.MapPost("/", AddAsync);
        group.MapGet("/", ListAsync);
        group.MapPatch("/{criterionId:guid}", UpdateStatusAsync);

        return routes;
    }

    private static async Task<IResult> AddAsync(
        string wsSlug,
        string projKey,
        int number,
        AddCriterionRequest? body,
        IProjectResolver projectResolver,
        IGetWorkItemHandler getHandler,
        IAddCriterionHandler addHandler,
        CancellationToken cancellationToken)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Description))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["description"] = ["Description is required."]
            });
        }
        if (body.Description.Length > 2048)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["description"] = ["Description must be at most 2048 characters."]
            });
        }

        var (project, workItem) = await Resolve(wsSlug, projKey, number, projectResolver, getHandler, cancellationToken);
        if (workItem is null)
        {
            return project is null
                ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Project not found")
                : Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Work item not found");
        }

        var result = await addHandler.HandleAsync(new AddCriterionCommand(workItem.Id, body.Description), cancellationToken);
        var location = $"/api/v1/workspaces/{wsSlug}/projects/{projKey}/work-items/{number}/acceptance-criteria/{result.Id.Value}";
        return Results.Created(location, new { id = result.Id.Value });
    }

    private static async Task<IResult> ListAsync(
        string wsSlug,
        string projKey,
        int number,
        IProjectResolver projectResolver,
        IGetWorkItemHandler getHandler,
        IListCriteriaHandler listHandler,
        CancellationToken cancellationToken)
    {
        var (project, workItem) = await Resolve(wsSlug, projKey, number, projectResolver, getHandler, cancellationToken);
        if (workItem is null)
        {
            return project is null
                ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Project not found")
                : Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Work item not found");
        }

        var rows = await listHandler.HandleAsync(new ListCriteriaQuery(workItem.Id), cancellationToken);
        return Results.Ok(rows.Select(ToResponse).ToList());
    }

    private static async Task<IResult> UpdateStatusAsync(
        string wsSlug,
        string projKey,
        int number,
        Guid criterionId,
        UpdateCriterionStatusRequest? body,
        IProjectResolver projectResolver,
        IGetWorkItemHandler getHandler,
        IUpdateCriterionStatusHandler updateHandler,
        CancellationToken cancellationToken)
    {
        if (body is null || body.UserId == Guid.Empty)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["userId"] = ["UserId is required."]
            });
        }

        var (project, workItem) = await Resolve(wsSlug, projKey, number, projectResolver, getHandler, cancellationToken);
        if (workItem is null)
        {
            return project is null
                ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Project not found")
                : Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Work item not found");
        }

        try
        {
            await updateHandler.HandleAsync(
                new UpdateCriterionStatusCommand(
                    new AcceptanceCriterionId(criterionId),
                    body.Status,
                    new UserId(body.UserId),
                    body.Comment),
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Criterion not found", detail: ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid update", detail: ex.Message);
        }

        return Results.NoContent();
    }

    private static async Task<(ProjectScope? Project, WorkItem? WorkItem)> Resolve(
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
            return (null, null);
        }
        var workItem = await getHandler.HandleAsync(new GetWorkItemQuery(project.ProjectId, number), cancellationToken);
        return (project, workItem);
    }

    private static AcceptanceCriterionResponse ToResponse(AcceptanceCriterion c) => new(
        c.Id.Value,
        c.WorkItemId.Value,
        c.Description,
        c.Status,
        c.CheckedByUserId?.Value,
        c.CheckedAt,
        c.Comment,
        c.CreatedAt,
        c.UpdatedAt);
}
