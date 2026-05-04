using System.Text.Json;
using DoTrack.Application.Auditing;
using DoTrack.Application.WorkItems;
using DoTrack.Application.Workspaces;
using DoTrack.Domain.Auditing;

namespace DoTrack.Api.Auditing;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet(
            "/api/v1/workspaces/{wsSlug}/projects/{projKey}/work-items/{number:int}/history",
            GetWorkItemHistoryAsync)
            .WithTags("Audit");
        return routes;
    }

    private static async Task<IResult> GetWorkItemHistoryAsync(
        string wsSlug,
        string projKey,
        int number,
        int? limit,
        IProjectResolver projectResolver,
        IGetWorkItemHandler getHandler,
        IGetEntityHistoryHandler historyHandler,
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

        var rows = await historyHandler.HandleAsync(
            new GetEntityHistoryQuery("WorkItem", workItem.Id.Value.ToString(), Math.Clamp(limit ?? 200, 1, 1000)),
            cancellationToken);

        var responses = rows.Select(ToResponse).ToList();
        return Results.Ok(responses);
    }

    private static AuditLogResponse ToResponse(AuditLog row)
    {
        IReadOnlyDictionary<string, string>? metadata = null;
        if (!string.IsNullOrEmpty(row.SourceMetadataJson))
        {
            try
            {
                metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(row.SourceMetadataJson);
            }
            catch (JsonException)
            {
                metadata = null;
            }
        }

        return new AuditLogResponse(
            row.Id.Value,
            row.EntityType,
            row.EntityId,
            row.ChangeType,
            row.ChangedByUserId?.Value,
            row.OccurredAt,
            row.Source,
            row.ChangeReason,
            metadata,
            row.FieldChanges);
    }
}
