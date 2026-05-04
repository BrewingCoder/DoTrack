using DoTrack.Application.WorkItems;
using DoTrack.Application.Workspaces;
using DoTrack.Domain.Identity;

namespace DoTrack.Api.WorkItems;

public sealed record WatchRequest(Guid UserId);

public static class WatcherEndpoints
{
    public static IEndpointRouteBuilder MapWatcherEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/workspaces/{wsSlug}/projects/{projKey}/work-items/{number:int}/watchers")
            .WithTags("Watchers");
        group.MapPost("/", AddWatcherAsync);
        group.MapDelete("/{userId:guid}", RemoveWatcherAsync);
        group.MapGet("/", ListWatchersAsync);

        routes.MapGet("/api/v1/users/{userId:guid}/my-work", MyWorkAsync).WithTags("MyWork");
        return routes;
    }

    private static async Task<IResult> AddWatcherAsync(
        string wsSlug,
        string projKey,
        int number,
        WatchRequest? body,
        IProjectResolver projectResolver,
        IGetWorkItemHandler getHandler,
        IWatchWorkItemHandler watchHandler,
        CancellationToken cancellationToken)
    {
        if (body is null || body.UserId == Guid.Empty)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["userId"] = ["Required."] });
        }
        var project = await projectResolver.ResolveAsync(wsSlug, projKey, cancellationToken);
        if (project is null)
        {
            return Results.Problem(statusCode: 404, title: "Project not found");
        }
        var workItem = await getHandler.HandleAsync(new GetWorkItemQuery(project.ProjectId, number), cancellationToken);
        if (workItem is null)
        {
            return Results.Problem(statusCode: 404, title: "Work item not found");
        }

        try
        {
            await watchHandler.HandleAsync(new WatchWorkItemCommand(workItem.Id, new UserId(body.UserId)), cancellationToken);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(statusCode: 404, title: "Reference missing", detail: ex.Message);
        }
    }

    private static async Task<IResult> RemoveWatcherAsync(
        string wsSlug,
        string projKey,
        int number,
        Guid userId,
        IProjectResolver projectResolver,
        IGetWorkItemHandler getHandler,
        IUnwatchWorkItemHandler unwatchHandler,
        CancellationToken cancellationToken)
    {
        var project = await projectResolver.ResolveAsync(wsSlug, projKey, cancellationToken);
        if (project is null)
        {
            return Results.Problem(statusCode: 404, title: "Project not found");
        }
        var workItem = await getHandler.HandleAsync(new GetWorkItemQuery(project.ProjectId, number), cancellationToken);
        if (workItem is null)
        {
            return Results.Problem(statusCode: 404, title: "Work item not found");
        }

        await unwatchHandler.HandleAsync(new UnwatchWorkItemCommand(workItem.Id, new UserId(userId)), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ListWatchersAsync(
        string wsSlug,
        string projKey,
        int number,
        IProjectResolver projectResolver,
        IGetWorkItemHandler getHandler,
        IListWatchersHandler listHandler,
        CancellationToken cancellationToken)
    {
        var project = await projectResolver.ResolveAsync(wsSlug, projKey, cancellationToken);
        if (project is null)
        {
            return Results.Problem(statusCode: 404, title: "Project not found");
        }
        var workItem = await getHandler.HandleAsync(new GetWorkItemQuery(project.ProjectId, number), cancellationToken);
        if (workItem is null)
        {
            return Results.Problem(statusCode: 404, title: "Work item not found");
        }
        var watchers = await listHandler.HandleAsync(new ListWatchersQuery(workItem.Id), cancellationToken);
        return Results.Ok(watchers.Select(u => u.Value).ToList());
    }

    private static async Task<IResult> MyWorkAsync(
        Guid userId,
        IMyWorkHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new MyWorkQuery(new UserId(userId)), cancellationToken);
        return Results.Ok(new
        {
            assigned = result.Assigned.Select(w => new { id = w.Id.Value, projectId = w.ProjectId.Value, number = w.Number, title = w.Title, state = w.State.ToString(), tier = w.Tier.ToString(), type = w.Type?.ToString() }).ToList(),
            reporting = result.Reporting.Select(w => new { id = w.Id.Value, projectId = w.ProjectId.Value, number = w.Number, title = w.Title, state = w.State.ToString(), tier = w.Tier.ToString(), type = w.Type?.ToString() }).ToList(),
            watching = result.Watching.Select(w => new { id = w.Id.Value, projectId = w.ProjectId.Value, number = w.Number, title = w.Title, state = w.State.ToString(), tier = w.Tier.ToString(), type = w.Type?.ToString() }).ToList()
        });
    }
}
