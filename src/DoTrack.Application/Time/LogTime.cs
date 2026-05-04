using DoTrack.Domain.Identity;
using DoTrack.Domain.Time;
using DoTrack.Domain.WorkItems;

namespace DoTrack.Application.Time;

public sealed record LogTimeCommand(
    WorkItemId WorkItemId,
    UserId UserId,
    DateTimeOffset StartedAt,
    TimeSpan Duration,
    string Description,
    bool Billable,
    string? ActivityType);

public sealed record LogTimeResult(TimeEntryId Id);

public interface ILogTimeHandler
{
    Task<LogTimeResult> HandleAsync(LogTimeCommand command, CancellationToken cancellationToken);
}
