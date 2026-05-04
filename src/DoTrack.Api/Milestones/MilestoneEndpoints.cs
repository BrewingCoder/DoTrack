using DoTrack.Api.WorkItems;
using DoTrack.Application.Milestones;
using DoTrack.Application.WorkItems;
using DoTrack.Application.Workspaces;
using DoTrack.Domain.Milestones;
using DoTrack.Domain.WorkItems;

namespace DoTrack.Api.Milestones;

public sealed record CreateMilestoneRequest(
    string Name, string? Description, DateOnly? TargetDate, decimal? HoursBudget, bool VisibleToClient);

public sealed record UpdateMilestoneRequest(
    string? Name, string? Description, DateOnly? TargetDate, decimal? HoursBudget,
    bool? VisibleToClient, MilestoneState? State);

public sealed record MilestoneResponse(
    Guid Id, string Name, string? Description, DateOnly? TargetDate,
    decimal? HoursBudget, bool VisibleToClient, MilestoneState State,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record AddScopeItemRequest(string ProjectKey, string WorkspaceSlug, int Number);

public sealed record MilestoneHealthResponse(
    Guid MilestoneId,
    decimal HoursLogged,
    decimal? HoursBudget,
    int ScopeTotal,
    int ScopeDone,
    decimal? BudgetPct,
    decimal ScopePct,
    decimal? HealthGap,
    decimal? ProjectedTotal,
    decimal? ProjectedOverage);

public static class MilestoneEndpoints
{
    public static IEndpointRouteBuilder MapMilestoneEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/milestones").WithTags("Milestones");
        group.MapPost("/", CreateAsync);
        group.MapGet("/", ListAsync);
        group.MapPatch("/{milestoneId:guid}", UpdateAsync);
        group.MapDelete("/{milestoneId:guid}", DeleteAsync);
        group.MapGet("/{milestoneId:guid}/scope", GetScopeAsync);
        group.MapPost("/{milestoneId:guid}/scope", AddScopeAsync);
        group.MapDelete("/{milestoneId:guid}/scope/{workItemId:guid}", RemoveScopeAsync);
        group.MapGet("/{milestoneId:guid}/health", GetHealthAsync);
        return routes;
    }

    private static async Task<IResult> CreateAsync(
        CreateMilestoneRequest? body,
        ICreateMilestoneHandler handler,
        CancellationToken cancellationToken)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Name))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Name is required."] });
        }
        if (body.HoursBudget is < 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["hoursBudget"] = ["Must be non-negative."] });
        }

        var result = await handler.HandleAsync(
            new CreateMilestoneCommand(body.Name, body.Description, body.TargetDate, body.HoursBudget, body.VisibleToClient),
            cancellationToken);
        return Results.Created($"/api/v1/milestones/{result.Id.Value}", new { id = result.Id.Value });
    }

    private static async Task<IResult> ListAsync(IListMilestonesHandler handler, CancellationToken cancellationToken)
    {
        var rows = await handler.HandleAsync(cancellationToken);
        return Results.Ok(rows.Select(ToResponse).ToList());
    }

    private static async Task<IResult> UpdateAsync(
        Guid milestoneId,
        UpdateMilestoneRequest? body,
        IUpdateMilestoneHandler handler,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["body"] = ["Body required."] });
        }
        try
        {
            await handler.HandleAsync(new UpdateMilestoneCommand(
                new MilestoneId(milestoneId), body.Name, body.Description, body.TargetDate,
                body.HoursBudget, body.VisibleToClient, body.State), cancellationToken);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Milestone not found", detail: ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid update", detail: ex.Message);
        }
    }

    private static async Task<IResult> DeleteAsync(
        Guid milestoneId,
        IDeleteMilestoneHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            await handler.HandleAsync(new DeleteMilestoneCommand(new MilestoneId(milestoneId)), cancellationToken);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Milestone not found", detail: ex.Message);
        }
    }

    private static async Task<IResult> GetScopeAsync(
        Guid milestoneId,
        IGetMilestoneScopeHandler handler,
        CancellationToken cancellationToken)
    {
        var items = await handler.HandleAsync(new GetMilestoneScopeQuery(new MilestoneId(milestoneId)), cancellationToken);
        // Each item carries its own ProjectId — the consumer can join by-ID,
        // but for v0 we just return the raw work items via WorkItemContractMapper.
        // Since WorkItemContractMapper requires the project key, we'd need to
        // resolve it. Quick-and-dirty: return Number + Id + ProjectId raw.
        var responses = items.Select(w => new
        {
            id = w.Id.Value,
            number = w.Number,
            projectId = w.ProjectId.Value,
            tier = w.Tier.ToString(),
            type = w.Type?.ToString(),
            state = w.State.ToString(),
            title = w.Title
        }).ToList();
        return Results.Ok(responses);
    }

    private static async Task<IResult> AddScopeAsync(
        Guid milestoneId,
        AddScopeItemRequest? body,
        IProjectResolver projectResolver,
        IGetWorkItemHandler getWorkItem,
        IAddScopeItemHandler addHandler,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["body"] = ["Body required."] });
        }

        var project = await projectResolver.ResolveAsync(body.WorkspaceSlug, body.ProjectKey, cancellationToken);
        if (project is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Project not found");
        }
        var workItem = await getWorkItem.HandleAsync(new GetWorkItemQuery(project.ProjectId, body.Number), cancellationToken);
        if (workItem is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Work item not found");
        }

        try
        {
            await addHandler.HandleAsync(new AddScopeItemCommand(new MilestoneId(milestoneId), workItem.Id), cancellationToken);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Scope add failed", detail: ex.Message);
        }
    }

    private static async Task<IResult> RemoveScopeAsync(
        Guid milestoneId,
        Guid workItemId,
        IRemoveScopeItemHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new RemoveScopeItemCommand(new MilestoneId(milestoneId), new WorkItemId(workItemId)), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetHealthAsync(
        Guid milestoneId,
        IGetMilestoneHealthHandler handler,
        CancellationToken cancellationToken)
    {
        var health = await handler.HandleAsync(new GetMilestoneHealthQuery(new MilestoneId(milestoneId)), cancellationToken);
        if (health is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Milestone not found");
        }
        return Results.Ok(new MilestoneHealthResponse(
            health.Id.Value, health.HoursLogged, health.HoursBudget, health.ScopeTotal, health.ScopeDone,
            health.BudgetPct, health.ScopePct, health.HealthGap, health.ProjectedTotal, health.ProjectedOverage));
    }

    private static MilestoneResponse ToResponse(Milestone m) => new(
        m.Id.Value, m.Name, m.Description, m.TargetDate, m.HoursBudget,
        m.VisibleToClient, m.State, m.CreatedAt, m.UpdatedAt);
}
