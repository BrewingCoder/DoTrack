using DoTrack.Application.Milestones;
using DoTrack.Domain.Milestones;
using DoTrack.Domain.WorkItems;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.Milestones;

public sealed class CreateMilestoneHandler(DoTrackDbContext db, TimeProvider timeProvider) : ICreateMilestoneHandler
{
    public async Task<CreateMilestoneResult> HandleAsync(CreateMilestoneCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var milestone = new Milestone(
            MilestoneId.New(), command.Name, command.Description,
            command.TargetDate, command.HoursBudget, command.VisibleToClient,
            timeProvider.GetUtcNow());
        db.Milestones.Add(milestone);
        await db.SaveChangesAsync(cancellationToken);
        return new CreateMilestoneResult(milestone.Id);
    }
}

public sealed class UpdateMilestoneHandler(DoTrackDbContext db, TimeProvider timeProvider) : IUpdateMilestoneHandler
{
    public async Task HandleAsync(UpdateMilestoneCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var milestone = await db.Milestones.SingleOrDefaultAsync(m => m.Id == command.MilestoneId, cancellationToken)
            ?? throw new InvalidOperationException($"Milestone '{command.MilestoneId.Value}' not found.");

        var now = timeProvider.GetUtcNow();
        milestone.Update(command.Name, command.Description, command.TargetDate, command.HoursBudget, command.VisibleToClient, now);
        if (command.State is { } state)
        {
            switch (state)
            {
                case MilestoneState.Completed: milestone.Complete(now); break;
                case MilestoneState.Cancelled: milestone.Cancel(now); break;
            }
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class DeleteMilestoneHandler(DoTrackDbContext db) : IDeleteMilestoneHandler
{
    public async Task HandleAsync(DeleteMilestoneCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var milestone = await db.Milestones.SingleOrDefaultAsync(m => m.Id == command.MilestoneId, cancellationToken)
            ?? throw new InvalidOperationException($"Milestone '{command.MilestoneId.Value}' not found.");
        // Clear scope rows first (NoAction FK at DB level).
        await db.MilestoneScope.Where(s => s.MilestoneId == milestone.Id).ExecuteDeleteAsync(cancellationToken);
        db.Milestones.Remove(milestone);
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ListMilestonesHandler(DoTrackDbContext db) : IListMilestonesHandler
{
    public async Task<IReadOnlyList<Milestone>> HandleAsync(CancellationToken cancellationToken)
    {
        var rows = await db.Milestones.ToListAsync(cancellationToken);
        return rows.OrderBy(m => m.TargetDate ?? DateOnly.MaxValue).ThenBy(m => m.Name).ToList();
    }
}

public sealed class AddScopeItemHandler(DoTrackDbContext db, TimeProvider timeProvider) : IAddScopeItemHandler
{
    public async Task HandleAsync(AddScopeItemCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var milestoneExists = await db.Milestones.AnyAsync(m => m.Id == command.MilestoneId, cancellationToken);
        if (!milestoneExists)
        {
            throw new InvalidOperationException($"Milestone '{command.MilestoneId.Value}' not found.");
        }
        var workItemExists = await db.WorkItems.AnyAsync(w => w.Id == command.WorkItemId, cancellationToken);
        if (!workItemExists)
        {
            throw new InvalidOperationException($"WorkItem '{command.WorkItemId.Value}' not found.");
        }
        var alreadyInScope = await db.MilestoneScope
            .AnyAsync(s => s.MilestoneId == command.MilestoneId && s.WorkItemId == command.WorkItemId, cancellationToken);
        if (alreadyInScope)
        {
            return;
        }
        db.MilestoneScope.Add(new MilestoneScope(command.MilestoneId, command.WorkItemId, timeProvider.GetUtcNow()));
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class RemoveScopeItemHandler(DoTrackDbContext db) : IRemoveScopeItemHandler
{
    public async Task HandleAsync(RemoveScopeItemCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await db.MilestoneScope
            .Where(s => s.MilestoneId == command.MilestoneId && s.WorkItemId == command.WorkItemId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}

public sealed class GetMilestoneScopeHandler(DoTrackDbContext db) : IGetMilestoneScopeHandler
{
    public async Task<IReadOnlyList<WorkItem>> HandleAsync(GetMilestoneScopeQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var rows = await (
            from s in db.MilestoneScope
            join w in db.WorkItems on s.WorkItemId equals w.Id
            where s.MilestoneId == query.MilestoneId
            select w
        ).ToListAsync(cancellationToken);
        return rows.OrderBy(w => w.Number).ToList();
    }
}

public sealed class GetMilestoneHealthHandler(DoTrackDbContext db) : IGetMilestoneHealthHandler
{
    public async Task<MilestoneHealth?> HandleAsync(GetMilestoneHealthQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var milestone = await db.Milestones.SingleOrDefaultAsync(m => m.Id == query.MilestoneId, cancellationToken);
        if (milestone is null)
        {
            return null;
        }

        var scope = await (
            from s in db.MilestoneScope
            join w in db.WorkItems on s.WorkItemId equals w.Id
            where s.MilestoneId == query.MilestoneId
            select w
        ).ToListAsync(cancellationToken);

        var scopeTotal = scope.Count;
        var scopeDone = scope.Count(w => w.State == Domain.WorkItems.WorkItemState.Accepted);
        var scopeIds = scope.Select(w => w.Id).ToList();

        // Hours logged across all scope items (TimeEntry.Duration).
        var entries = await db.TimeEntries
            .Where(t => scopeIds.Contains(t.WorkItemId))
            .Select(t => t.Duration)
            .ToListAsync(cancellationToken);
        var hoursLogged = (decimal)entries.Sum(d => d.TotalHours);

        decimal? budgetPct = milestone.HoursBudget > 0
            ? Math.Round(hoursLogged / milestone.HoursBudget.Value, 4)
            : null;
        var scopePct = scopeTotal > 0
            ? Math.Round((decimal)scopeDone / scopeTotal, 4)
            : 0m;
        decimal? healthGap = budgetPct.HasValue ? budgetPct - scopePct : null;
        decimal? projectedTotal = scopePct > 0
            ? Math.Round(hoursLogged / scopePct, 2)
            : null;
        decimal? projectedOverage = projectedTotal.HasValue && milestone.HoursBudget.HasValue
            ? Math.Round(projectedTotal.Value - milestone.HoursBudget.Value, 2)
            : null;

        return new MilestoneHealth(
            milestone.Id,
            Math.Round(hoursLogged, 2),
            milestone.HoursBudget,
            scopeTotal,
            scopeDone,
            budgetPct,
            scopePct,
            healthGap,
            projectedTotal,
            projectedOverage);
    }
}
