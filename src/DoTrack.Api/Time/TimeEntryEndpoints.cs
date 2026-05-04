using DoTrack.Application.Time;
using DoTrack.Application.WorkItems;
using DoTrack.Application.Workspaces;
using DoTrack.Domain.Identity;
using DoTrack.Domain.Time;

namespace DoTrack.Api.Time;

public static class TimeEntryEndpoints
{
    public static IEndpointRouteBuilder MapTimeEntryEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/workspaces/{wsSlug}/projects/{projKey}/work-items/{number:int}/time-entries")
            .WithTags("TimeEntries");

        group.MapPost("/", LogAsync);
        group.MapGet("/", ListAsync);

        return routes;
    }

    private static async Task<IResult> LogAsync(
        string wsSlug,
        string projKey,
        int number,
        LogTimeRequest? body,
        IProjectResolver projectResolver,
        IGetWorkItemHandler getHandler,
        ILogTimeHandler logHandler,
        CancellationToken cancellationToken)
    {
        var validationErrors = LogTimeRequestValidator.Validate(body);
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

        var result = await logHandler.HandleAsync(
            new LogTimeCommand(
                workItem.Id,
                new UserId(body!.UserId),
                body.StartedAt,
                TimeSpan.FromMinutes(body.DurationMinutes),
                body.Description,
                body.Billable,
                body.ActivityType),
            cancellationToken);

        var location = $"/api/v1/workspaces/{wsSlug}/projects/{projKey}/work-items/{number}/time-entries/{result.Id.Value}";
        return Results.Created(location, new TimeEntryResponse(
            result.Id.Value, workItem.Id.Value, body.UserId, body.StartedAt, body.DurationMinutes,
            body.Description, body.Billable, body.ActivityType, DateTimeOffset.UtcNow, null));
    }

    private static async Task<IResult> ListAsync(
        string wsSlug,
        string projKey,
        int number,
        IProjectResolver projectResolver,
        IGetWorkItemHandler getHandler,
        IListTimeEntriesHandler listHandler,
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

        var entries = await listHandler.HandleAsync(
            new ListTimeEntriesQuery(workItem.Id),
            cancellationToken);

        return Results.Ok(entries.Select(ToResponse).ToList());
    }

    private static TimeEntryResponse ToResponse(TimeEntry t) => new(
        t.Id.Value,
        t.WorkItemId.Value,
        t.UserId.Value,
        t.StartedAt,
        (int)t.Duration.TotalMinutes,
        t.Description,
        t.Billable,
        t.ActivityType,
        t.CreatedAt,
        t.LastEditedAt);
}
