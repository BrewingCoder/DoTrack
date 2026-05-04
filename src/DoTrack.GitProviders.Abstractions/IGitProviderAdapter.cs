namespace DoTrack.GitProviders.Abstractions;

public interface IGitProviderAdapter
{
    string ProviderId { get; }
    string DisplayName { get; }

    bool VerifySignature(WebhookRequest request, string secret);

    Task<IReadOnlyList<GitWebhookEvent>> ParseWebhookAsync(WebhookRequest request, CancellationToken cancellationToken);
}
