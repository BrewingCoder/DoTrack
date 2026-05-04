using System.Text.Json;
using DoTrack.Automation.Abstractions;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DoTrack.Workers;

public sealed class OutboxDispatcherOptions
{
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);
    public int BatchSize { get; set; } = 50;
    public int MaxAttempts { get; set; } = 5;
}

public sealed class OutboxDispatcherService(
    IServiceProvider services,
    IAutomationProvider provider,
    OutboxDispatcherOptions options,
    TimeProvider timeProvider,
    ILogger<OutboxDispatcherService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox dispatcher started (poll {Interval})", options.PollInterval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var dispatched = await PollAsync(stoppingToken);
                if (dispatched == 0)
                {
                    await Task.Delay(options.PollInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox dispatcher tick failed");
                await Task.Delay(options.PollInterval, stoppingToken);
            }
        }
    }

    private async Task<int> PollAsync(CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DoTrackDbContext>();

        var pending = await db.OutboxMessages
            .Where(m => m.DeliveredAt == null && m.Attempts < options.MaxAttempts)
            .OrderBy(m => m.CreatedAt)
            .Take(options.BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return 0;
        }

        var dispatched = 0;
        foreach (var message in pending)
        {
            try
            {
                var evt = new AutomationEvent
                {
                    EventId = message.Id.Value,
                    EventType = message.EventType,
                    OccurredAt = message.CreatedAt,
                    ProjectKey = message.ProjectKey,
                    Payload = JsonSerializer.Deserialize<Dictionary<string, object?>>(message.PayloadJson)
                              ?? new Dictionary<string, object?>()
                };
                await provider.DeliverEventAsync(evt, cancellationToken);
                message.MarkDelivered(timeProvider.GetUtcNow());
                dispatched++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.MarkAttemptFailed(ex.Message[..Math.Min(2048, ex.Message.Length)], timeProvider.GetUtcNow());
                logger.LogWarning(ex,
                    "Failed to deliver outbox message {MessageId} ({EventType}); attempt {Attempts}",
                    message.Id, message.EventType, message.Attempts);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return dispatched;
    }
}
