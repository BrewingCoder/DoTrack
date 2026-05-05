using DoTrack.Application.Comments;
using DoTrack.Application.WorkItems;
using DoTrack.Application.Workspaces;
using DoTrack.Domain.Comments;
using DoTrack.Domain.Identity;

namespace DoTrack.Api.Comments;

public static class CommentEndpoints
{
    public static IEndpointRouteBuilder MapCommentEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/workspaces/{wsSlug}/projects/{projKey}/work-items/{number:int}/comments")
            .WithTags("Comments");

        group.MapPost("/", AddAsync);
        group.MapGet("/", ListAsync).Produces<List<CommentResponse>>(StatusCodes.Status200OK);

        return routes;
    }

    private static async Task<IResult> AddAsync(
        string wsSlug,
        string projKey,
        int number,
        AddCommentRequest? body,
        IProjectResolver projectResolver,
        IGetWorkItemHandler getHandler,
        IAddCommentHandler addCommentHandler,
        CancellationToken cancellationToken)
    {
        var validationErrors = AddCommentRequestValidator.Validate(body);
        if (validationErrors is not null)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var project = await projectResolver.ResolveAsync(wsSlug, projKey, cancellationToken);
        if (project is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Project not found");
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

        var result = await addCommentHandler.HandleAsync(
            new AddCommentCommand(workItem.Id, new UserId(body!.AuthorId), body.Body, body.IsInternal),
            cancellationToken);

        var location = $"/api/v1/workspaces/{wsSlug}/projects/{projKey}/work-items/{number}/comments/{result.Id.Value}";
        var response = new CommentResponse(
            result.Id.Value, workItem.Id.Value, body.AuthorId, body.Body, body.IsInternal,
            DateTimeOffset.UtcNow, null);
        return Results.Created(location, response);
    }

    private static async Task<IResult> ListAsync(
        string wsSlug,
        string projKey,
        int number,
        bool? includeInternal,
        IProjectResolver projectResolver,
        IGetWorkItemHandler getHandler,
        IListCommentsHandler listHandler,
        CancellationToken cancellationToken)
    {
        var project = await projectResolver.ResolveAsync(wsSlug, projKey, cancellationToken);
        if (project is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Project not found");
        }

        var workItem = await getHandler.HandleAsync(
            new GetWorkItemQuery(project.ProjectId, number),
            cancellationToken);
        if (workItem is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Work item not found");
        }

        var comments = await listHandler.HandleAsync(
            new ListCommentsQuery(workItem.Id, includeInternal ?? false),
            cancellationToken);

        var response = comments.Select(ToResponse).ToList();
        return Results.Ok(response);
    }

    private static CommentResponse ToResponse(Comment c) => new(
        c.Id.Value,
        c.WorkItemId.Value,
        c.AuthorId.Value,
        c.Body,
        c.IsInternal,
        c.CreatedAt,
        c.UpdatedAt);
}
