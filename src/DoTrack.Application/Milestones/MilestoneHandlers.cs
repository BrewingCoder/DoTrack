using DoTrack.Domain.Milestones;
using DoTrack.Domain.WorkItems;

namespace DoTrack.Application.Milestones;

public sealed record CreateMilestoneCommand(
    string Name, string? Description, DateOnly? TargetDate, decimal? HoursBudget, bool VisibleToClient);
public sealed record CreateMilestoneResult(MilestoneId Id);
public interface ICreateMilestoneHandler
{
    Task<CreateMilestoneResult> HandleAsync(CreateMilestoneCommand command, CancellationToken cancellationToken);
}

public sealed record UpdateMilestoneCommand(
    MilestoneId MilestoneId, string? Name, string? Description, DateOnly? TargetDate,
    decimal? HoursBudget, bool? VisibleToClient, MilestoneState? State);
public interface IUpdateMilestoneHandler
{
    Task HandleAsync(UpdateMilestoneCommand command, CancellationToken cancellationToken);
}

public sealed record DeleteMilestoneCommand(MilestoneId MilestoneId);
public interface IDeleteMilestoneHandler
{
    Task HandleAsync(DeleteMilestoneCommand command, CancellationToken cancellationToken);
}

public interface IListMilestonesHandler
{
    Task<IReadOnlyList<Milestone>> HandleAsync(CancellationToken cancellationToken);
}

public sealed record AddScopeItemCommand(MilestoneId MilestoneId, WorkItemId WorkItemId);
public interface IAddScopeItemHandler
{
    Task HandleAsync(AddScopeItemCommand command, CancellationToken cancellationToken);
}

public sealed record RemoveScopeItemCommand(MilestoneId MilestoneId, WorkItemId WorkItemId);
public interface IRemoveScopeItemHandler
{
    Task HandleAsync(RemoveScopeItemCommand command, CancellationToken cancellationToken);
}

public sealed record GetMilestoneScopeQuery(MilestoneId MilestoneId);
public interface IGetMilestoneScopeHandler
{
    Task<IReadOnlyList<WorkItem>> HandleAsync(GetMilestoneScopeQuery query, CancellationToken cancellationToken);
}

public sealed record MilestoneHealth(
    MilestoneId Id,
    decimal HoursLogged,
    decimal? HoursBudget,
    int ScopeTotal,
    int ScopeDone,
    decimal? BudgetPct,
    decimal ScopePct,
    decimal? HealthGap,
    decimal? ProjectedTotal,
    decimal? ProjectedOverage);

public sealed record GetMilestoneHealthQuery(MilestoneId MilestoneId);
public interface IGetMilestoneHealthHandler
{
    Task<MilestoneHealth?> HandleAsync(GetMilestoneHealthQuery query, CancellationToken cancellationToken);
}
