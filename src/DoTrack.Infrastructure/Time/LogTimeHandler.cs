using DoTrack.Application.Time;
using DoTrack.Domain.Time;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.Time;

public sealed class LogTimeHandler(DoTrackDbContext db, TimeProvider timeProvider) : ILogTimeHandler
{
    public async Task<LogTimeResult> HandleAsync(LogTimeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var workItemExists = await db.WorkItems.AnyAsync(w => w.Id == command.WorkItemId, cancellationToken);
        if (!workItemExists)
        {
            throw new InvalidOperationException($"WorkItem '{command.WorkItemId.Value}' not found.");
        }

        if (command.Duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "Duration must be positive.");
        }
        if (string.IsNullOrWhiteSpace(command.Description))
        {
            throw new ArgumentException("Description is required for DCAA-aligned timekeeping.", nameof(command));
        }

        var entry = new TimeEntry(
            TimeEntryId.New(),
            command.WorkItemId,
            command.UserId,
            command.StartedAt,
            command.Duration,
            command.Description,
            command.Billable,
            command.ActivityType,
            timeProvider.GetUtcNow());

        db.TimeEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);

        return new LogTimeResult(entry.Id);
    }
}
