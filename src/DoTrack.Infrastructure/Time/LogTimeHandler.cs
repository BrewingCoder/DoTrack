using DoTrack.Application.Time;
using DoTrack.Domain.Time;
using DoTrack.Infrastructure.Outbox;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.Time;

public sealed class LogTimeHandler(DoTrackDbContext db, TimeProvider timeProvider, OutboxEmitter outbox) : ILogTimeHandler
{
    public async Task<LogTimeResult> HandleAsync(LogTimeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var workItem = await db.WorkItems.SingleOrDefaultAsync(w => w.Id == command.WorkItemId, cancellationToken)
            ?? throw new InvalidOperationException($"WorkItem '{command.WorkItemId.Value}' not found.");

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

        var project = await db.Projects.SingleAsync(p => p.Id == workItem.ProjectId, cancellationToken);
        outbox.Emit("time.logged", project.Key, new
        {
            timeEntryId = entry.Id.Value,
            workItemId = workItem.Id.Value,
            projectKey = project.Key,
            number = workItem.Number,
            key = $"{project.Key}-{workItem.Number}",
            userId = entry.UserId.Value,
            startedAt = entry.StartedAt,
            durationMinutes = (int)entry.Duration.TotalMinutes,
            description = entry.Description,
            billable = entry.Billable,
            activityType = entry.ActivityType
        });

        await db.SaveChangesAsync(cancellationToken);

        return new LogTimeResult(entry.Id);
    }
}
