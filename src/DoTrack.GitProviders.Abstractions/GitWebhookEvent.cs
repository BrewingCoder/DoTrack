namespace DoTrack.GitProviders.Abstractions;

public abstract record GitWebhookEvent
{
    public required DateTimeOffset OccurredAt { get; init; }
    public required string ProviderId { get; init; }
    public required string Repository { get; init; }
}

public sealed record GitCommit(
    string Sha,
    string Message,
    string AuthorName,
    string AuthorEmail,
    DateTimeOffset CommittedAt,
    string? Url);

public sealed record CommitPushed : GitWebhookEvent
{
    public required string Branch { get; init; }
    public required IReadOnlyList<GitCommit> Commits { get; init; }
}

public enum PullRequestAction
{
    Opened = 1,
    Updated = 2,
    Merged = 3,
    Closed = 4
}

public sealed record PullRequestEvent : GitWebhookEvent
{
    public required PullRequestAction Action { get; init; }
    public required int Number { get; init; }
    public required string Title { get; init; }
    public string? Body { get; init; }
    public required string SourceBranch { get; init; }
    public required string TargetBranch { get; init; }
    public required string Author { get; init; }
    public string? Url { get; init; }
}
