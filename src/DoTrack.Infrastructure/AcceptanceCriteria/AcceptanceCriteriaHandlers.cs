using DoTrack.Application.AcceptanceCriteria;
using DoTrack.Domain.WorkItems;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.AcceptanceCriteria;

public sealed class AddCriterionHandler(DoTrackDbContext db, TimeProvider timeProvider) : IAddCriterionHandler
{
    public async Task<AddCriterionResult> HandleAsync(AddCriterionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var workItemExists = await db.WorkItems.AnyAsync(w => w.Id == command.WorkItemId, cancellationToken);
        if (!workItemExists)
        {
            throw new InvalidOperationException($"WorkItem '{command.WorkItemId.Value}' not found.");
        }

        var criterion = new AcceptanceCriterion(
            AcceptanceCriterionId.New(),
            command.WorkItemId,
            command.Description,
            timeProvider.GetUtcNow());

        db.AcceptanceCriteria.Add(criterion);
        await db.SaveChangesAsync(cancellationToken);
        return new AddCriterionResult(criterion.Id);
    }
}

public sealed class UpdateCriterionStatusHandler(DoTrackDbContext db, TimeProvider timeProvider)
    : IUpdateCriterionStatusHandler
{
    public async Task HandleAsync(UpdateCriterionStatusCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var criterion = await db.AcceptanceCriteria
            .SingleOrDefaultAsync(c => c.Id == command.CriterionId, cancellationToken)
            ?? throw new InvalidOperationException($"AcceptanceCriterion '{command.CriterionId.Value}' not found.");

        var now = timeProvider.GetUtcNow();
        switch (command.NewStatus)
        {
            case AcceptanceCriterionStatus.Met:
                criterion.MarkMet(command.UserId, now, command.Comment);
                break;
            case AcceptanceCriterionStatus.Waived:
                criterion.Waive(command.UserId, now, command.Comment ?? string.Empty);
                break;
            case AcceptanceCriterionStatus.Pending:
                criterion.ResetToPending(now);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), $"Unknown status '{command.NewStatus}'.");
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ListCriteriaHandler(DoTrackDbContext db) : IListCriteriaHandler
{
    public async Task<IReadOnlyList<AcceptanceCriterion>> HandleAsync(ListCriteriaQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var rows = await db.AcceptanceCriteria
            .Where(c => c.WorkItemId == query.WorkItemId)
            .ToListAsync(cancellationToken);
        return rows.OrderBy(c => c.CreatedAt).ToList();
    }
}
