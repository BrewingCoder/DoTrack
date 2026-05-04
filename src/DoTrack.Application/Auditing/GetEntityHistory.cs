using DoTrack.Domain.Auditing;

namespace DoTrack.Application.Auditing;

public sealed record GetEntityHistoryQuery(string EntityType, string EntityId, int Limit = 200);

public interface IGetEntityHistoryHandler
{
    Task<IReadOnlyList<AuditLog>> HandleAsync(GetEntityHistoryQuery query, CancellationToken cancellationToken);
}
