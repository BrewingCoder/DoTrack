using System.Text.RegularExpressions;
using DoTrack.Application.Comments;
using DoTrack.Application.Webhooks;
using DoTrack.Application.WorkItems;
using DoTrack.Domain.Identity;
using DoTrack.Domain.WorkItems;
using DoTrack.GitProviders.Abstractions;
using Microsoft.Extensions.Logging;

namespace DoTrack.Infrastructure.Webhooks;

public sealed partial class WebhookEventDispatcher(
    IFindByIssueKeyHandler findByIssueKey,
    IAddCommentHandler addComment,
    IUpdateWorkItemHandler updateWorkItem,
    ILogger<WebhookEventDispatcher> logger) : IWebhookEventDispatcher
{
    [GeneratedRegex(@"#(fixed|resolved|closed|in-progress|in_progress)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SmartCommitDirective();

    public async Task DispatchAsync(IReadOnlyList<GitWebhookEvent> events, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(events);

        foreach (var evt in events)
        {
            switch (evt)
            {
                case CommitPushed push:
                    foreach (var commit in push.Commits)
                    {
                        await DispatchCommitAsync(push, commit, cancellationToken);
                    }
                    break;
                case PullRequestEvent pr:
                    await DispatchPullRequestAsync(pr, cancellationToken);
                    break;
                default:
                    logger.LogDebug("Skipping unhandled webhook event type {EventType}", evt.GetType().Name);
                    break;
            }
        }
    }

    private async Task DispatchCommitAsync(CommitPushed push, GitCommit commit, CancellationToken cancellationToken)
    {
        var keys = IssueKeyDetector.Extract(commit.Message);
        if (keys.Count == 0)
        {
            return;
        }

        // Smart-commit directive applies to every key in the commit (if present).
        var directive = SmartCommitDirective().Match(commit.Message);
        WorkItemState? targetState = directive.Success
            ? directive.Groups[1].Value.ToLowerInvariant() switch
            {
                "fixed" or "resolved" or "closed" => WorkItemState.Accepted,
                "in-progress" or "in_progress" => WorkItemState.InProgress,
                _ => null
            }
            : null;

        foreach (var key in keys)
        {
            var (projectKey, number) = SplitKey(key);
            if (projectKey is null)
            {
                continue;
            }

            var workItem = await findByIssueKey.HandleAsync(new FindByIssueKeyQuery(projectKey, number), cancellationToken);
            if (workItem is null)
            {
                logger.LogInformation("Webhook references unknown work item {Key}", key);
                continue;
            }

            // Comment summarising the commit
            var commentBody = $"Linked from commit `{commit.Sha[..Math.Min(7, commit.Sha.Length)]}` " +
                              $"in {push.Repository}@{push.Branch} by {commit.AuthorName}: {commit.Message}";
            try
            {
                await addComment.HandleAsync(
                    new AddCommentCommand(workItem.Id, GetSystemAuthor(workItem), commentBody, IsInternal: false),
                    cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Failed to add commit comment to {Key}", key);
            }

            if (targetState is { } newState)
            {
                try
                {
                    await updateWorkItem.HandleAsync(
                        new UpdateWorkItemCommand(workItem.Id, null, null, null, null, newState),
                        cancellationToken);
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
                {
                    logger.LogWarning(ex, "Failed to transition {Key} to {State}", key, newState);
                }
            }
        }
    }

    private async Task DispatchPullRequestAsync(PullRequestEvent pr, CancellationToken cancellationToken)
    {
        var combined = $"{pr.Title}\n{pr.Body}\n{pr.SourceBranch}";
        var keys = IssueKeyDetector.Extract(combined);
        if (keys.Count == 0)
        {
            return;
        }

        var commentBody = $"PR `{pr.Repository}#{pr.Number}` ({pr.Action}) by {pr.Author}: {pr.Title}" +
                          (string.IsNullOrEmpty(pr.Url) ? "" : $" — {pr.Url}");

        foreach (var key in keys)
        {
            var (projectKey, number) = SplitKey(key);
            if (projectKey is null)
            {
                continue;
            }
            var workItem = await findByIssueKey.HandleAsync(new FindByIssueKeyQuery(projectKey, number), cancellationToken);
            if (workItem is null)
            {
                continue;
            }
            try
            {
                await addComment.HandleAsync(
                    new AddCommentCommand(workItem.Id, GetSystemAuthor(workItem), commentBody, IsInternal: false),
                    cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Failed to add PR comment to {Key}", key);
            }

            if (pr.Action == PullRequestAction.Merged)
            {
                try
                {
                    await updateWorkItem.HandleAsync(
                        new UpdateWorkItemCommand(workItem.Id, null, null, null, null, WorkItemState.Accepted),
                        cancellationToken);
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
                {
                    logger.LogWarning(ex, "Failed to transition {Key} to Accepted on merge", key);
                }
            }
        }
    }

    private static (string? ProjectKey, int Number) SplitKey(string key)
    {
        var dashIndex = key.LastIndexOf('-');
        if (dashIndex <= 0 || dashIndex == key.Length - 1)
        {
            return (null, 0);
        }
        if (!int.TryParse(key[(dashIndex + 1)..], out var number))
        {
            return (null, 0);
        }
        return (key[..dashIndex], number);
    }

    // No auth yet, so we cannot attribute the action to a real user. Until that
    // lands, the auto-comment is authored by the work item's reporter as a
    // pragmatic stand-in. Once auth is wired, a dedicated "system" or per-
    // commit-author user will replace this.
    private static UserId GetSystemAuthor(WorkItem workItem) => workItem.ReporterId;
}
