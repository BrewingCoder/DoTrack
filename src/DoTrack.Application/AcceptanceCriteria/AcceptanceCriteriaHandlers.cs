using DoTrack.Domain.Identity;
using DoTrack.Domain.WorkItems;

namespace DoTrack.Application.AcceptanceCriteria;

public sealed record AddCriterionCommand(WorkItemId WorkItemId, string Description);
public sealed record AddCriterionResult(AcceptanceCriterionId Id);

public interface IAddCriterionHandler
{
    Task<AddCriterionResult> HandleAsync(AddCriterionCommand command, CancellationToken cancellationToken);
}

public sealed record UpdateCriterionStatusCommand(
    AcceptanceCriterionId CriterionId,
    AcceptanceCriterionStatus NewStatus,
    UserId UserId,
    string? Comment);

public interface IUpdateCriterionStatusHandler
{
    Task HandleAsync(UpdateCriterionStatusCommand command, CancellationToken cancellationToken);
}

public sealed record ListCriteriaQuery(WorkItemId WorkItemId);

public interface IListCriteriaHandler
{
    Task<IReadOnlyList<AcceptanceCriterion>> HandleAsync(ListCriteriaQuery query, CancellationToken cancellationToken);
}
