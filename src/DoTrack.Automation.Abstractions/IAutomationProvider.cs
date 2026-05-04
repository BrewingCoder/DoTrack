namespace DoTrack.Automation.Abstractions;

public interface IAutomationProvider
{
    string ProviderId { get; }
    string DisplayName { get; }

    Task DeliverEventAsync(AutomationEvent evt, CancellationToken cancellationToken);

    Task<bool> IsHealthyAsync(CancellationToken cancellationToken);
}
