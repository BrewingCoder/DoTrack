namespace DoTrack.GitProviders.Abstractions;

public sealed record WebhookRequest(
    string EventType,
    IReadOnlyDictionary<string, string> Headers,
    string Body);
