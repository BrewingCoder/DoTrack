using DoTrack.Application.WorkItems;
using DoTrack.Application.Workspaces;
using DoTrack.Domain.WorkItems;

namespace DoTrack.Api.WorkItems;

public sealed record AddLinkRequest(
    string TargetWorkspaceSlug,
    string TargetProjectKey,
    int TargetNumber,
    WorkItemLinkType LinkType);

public sealed record WorkItemLinkResponse(
    Guid Id,
    Guid OtherWorkItemId,
    WorkItemLinkType LinkType,
    bool IsOutbound,
    DateTimeOffset CreatedAt);

public static class WorkItemLinkEndpoints
{
    public static IEndpointRouteBuilder MapWorkItemLinkEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/workspaces/{wsSlug}/projects/{projKey}/work-items/{number:int}/links")
            .WithTags("WorkItemLinks");
        group.MapPost("/", AddAsync);
        group.MapGet("/", ListAsync);
        routes.MapDelete("/api/v1/work-item-links/{linkId:guid}", RemoveAsync).WithTags("WorkItemLinks");
        return routes;
    }

    private static async Task<IResult> AddAsync(
        string wsSlug,
        string projKey,
        int number,
        AddLinkRequest? body,
        IProjectResolver projectResolver,
        IGetWorkItemHandler getHandler,
        IAddWorkItemLinkHandler addHandler,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["body"] = ["Body required."] });
        }

        var sourceProject = await projectResolver.ResolveAsync(wsSlug, projKey, cancellationToken);
        if (sourceProject is null)
        {
            return Results.Problem(statusCode: 404, title: "Source project not found");
        }
        var source = await getHandler.HandleAsync(new GetWorkItemQuery(sourceProject.ProjectId, number), cancellationToken);
        if (source is null)
        {
            return Results.Problem(statusCode: 404, title: "Source work item not found");
        }

        var targetProject = await projectResolver.ResolveAsync(body.TargetWorkspaceSlug, body.TargetProjectKey, cancellationToken);
        if (targetProject is null)
        {
            return Results.Problem(statusCode: 404, title: "Target project not found");
        }
        var target = await getHandler.HandleAsync(new GetWorkItemQuery(targetProject.ProjectId, body.TargetNumber), cancellationToken);
        if (target is null)
        {
            return Results.Problem(statusCode: 404, title: "Target work item not found");
        }

        try
        {
            var result = await addHandler.HandleAsync(
                new AddWorkItemLinkCommand(source.Id, target.Id, body.LinkType, null),
                cancellationToken);
            return Results.Created($"/api/v1/work-item-links/{result.Id.Value}", new { id = result.Id.Value });
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(statusCode: 400, title: "Invalid link", detail: ex.Message);
        }
    }

    private static async Task<IResult> ListAsync(
        string wsSlug,
        string projKey,
        int number,
        IProjectResolver projectResolver,
        IGetWorkItemHandler getHandler,
        IListWorkItemLinksHandler listHandler,
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

        var links = await listHandler.HandleAsync(new ListWorkItemLinksQuery(workItem.Id), cancellationToken);
        return Results.Ok(links.Select(v => new WorkItemLinkResponse(
            v.Id.Value, v.OtherWorkItemId.Value, v.LinkType, v.IsOutbound, v.CreatedAt)).ToList());
    }

    private static async Task<IResult> RemoveAsync(
        Guid linkId,
        IRemoveWorkItemLinkHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new RemoveWorkItemLinkCommand(new WorkItemLinkId(linkId)), cancellationToken);
        return Results.NoContent();
    }
}
