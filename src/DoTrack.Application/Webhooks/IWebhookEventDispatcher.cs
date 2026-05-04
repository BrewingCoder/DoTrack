using DoTrack.GitProviders.Abstractions;

namespace DoTrack.Application.Webhooks;

public interface IWebhookEventDispatcher
{
    Task DispatchAsync(IReadOnlyList<GitWebhookEvent> events, CancellationToken cancellationToken);
}
