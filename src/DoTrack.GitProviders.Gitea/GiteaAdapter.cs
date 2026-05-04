using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DoTrack.GitProviders.Abstractions;

namespace DoTrack.GitProviders.Gitea;

/// <summary>
/// Implements IGitProviderAdapter for Gitea — and, by virtue of API
/// compatibility through Gitea 1.20 / Forgejo 1.20, also for Forgejo.
/// Webhook payload shapes are very close to GitHub's but with a few key
/// differences: signature header is X-Gitea-Signature (raw hex, no
/// "sha256=" prefix), pull_request "merged" lives directly on the PR
/// object the same way, and the action vocabulary overlaps almost
/// entirely.
/// </summary>
public sealed class GiteaAdapter : IGitProviderAdapter
{
    public string ProviderId => "gitea";
    public string DisplayName => "Gitea / Forgejo";

    public bool VerifySignature(WebhookRequest request, string secret)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(secret);

        if (!request.Headers.TryGetValue("X-Gitea-Signature", out var headerValue)
            && !request.Headers.TryGetValue("x-gitea-signature", out headerValue))
        {
            return false;
        }

        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(request.Body);

        using var hmac = new HMACSHA256(keyBytes);
        var computedHash = hmac.ComputeHash(bodyBytes);
        var computedHex = Convert.ToHexStringLower(computedHash);

        var expected = headerValue.ToLowerInvariant();
        if (expected.Length != computedHex.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(computedHex));
    }

    public Task<IReadOnlyList<GitWebhookEvent>> ParseWebhookAsync(
        WebhookRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var eventType = request.EventType?.ToLowerInvariant();
        IReadOnlyList<GitWebhookEvent> events = eventType switch
        {
            "push" => ParsePush(request.Body),
            "pull_request" => ParsePullRequest(request.Body),
            _ => Array.Empty<GitWebhookEvent>()
        };
        return Task.FromResult(events);
    }

    private List<GitWebhookEvent> ParsePush(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var refValue = root.TryGetProperty("ref", out var refEl) ? refEl.GetString() ?? "" : "";
        var branch = refValue.StartsWith("refs/heads/", StringComparison.Ordinal)
            ? refValue["refs/heads/".Length..]
            : refValue;

        var repo = root.TryGetProperty("repository", out var repoEl)
                   && repoEl.TryGetProperty("full_name", out var fullName)
            ? fullName.GetString() ?? ""
            : "";

        var commits = new List<GitCommit>();
        if (root.TryGetProperty("commits", out var commitsEl) && commitsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var c in commitsEl.EnumerateArray())
            {
                var sha = c.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
                var message = c.TryGetProperty("message", out var msgEl) ? msgEl.GetString() ?? "" : "";
                var authorName = c.TryGetProperty("author", out var authEl)
                                 && authEl.TryGetProperty("name", out var nameEl)
                    ? nameEl.GetString() ?? ""
                    : "";
                var authorEmail = c.TryGetProperty("author", out var authEl2)
                                  && authEl2.TryGetProperty("email", out var emailEl)
                    ? emailEl.GetString() ?? ""
                    : "";
                var ts = c.TryGetProperty("timestamp", out var tsEl)
                         && tsEl.ValueKind == JsonValueKind.String
                         && DateTimeOffset.TryParse(tsEl.GetString(), out var parsed)
                    ? parsed
                    : DateTimeOffset.UtcNow;
                var url = c.TryGetProperty("url", out var urlEl) ? urlEl.GetString() : null;

                commits.Add(new GitCommit(sha, message, authorName, authorEmail, ts, url));
            }
        }

        return new List<GitWebhookEvent>
        {
            new CommitPushed
            {
                OccurredAt = DateTimeOffset.UtcNow,
                ProviderId = ProviderId,
                Repository = repo,
                Branch = branch,
                Commits = commits
            }
        };
    }

    private List<GitWebhookEvent> ParsePullRequest(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var actionStr = root.TryGetProperty("action", out var actionEl) ? actionEl.GetString() ?? "" : "";
        var prNumber = root.TryGetProperty("number", out var numEl) ? numEl.GetInt32() : 0;
        var repo = root.TryGetProperty("repository", out var repoEl)
                   && repoEl.TryGetProperty("full_name", out var fullName)
            ? fullName.GetString() ?? ""
            : "";

        if (!root.TryGetProperty("pull_request", out var pr))
        {
            return new List<GitWebhookEvent>();
        }

        var title = pr.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? "" : "";
        var prBody = pr.TryGetProperty("body", out var bodyEl) ? bodyEl.GetString() : null;
        var sourceBranch = pr.TryGetProperty("head", out var headEl)
                           && headEl.TryGetProperty("ref", out var headRefEl)
            ? headRefEl.GetString() ?? ""
            : "";
        var targetBranch = pr.TryGetProperty("base", out var baseEl)
                           && baseEl.TryGetProperty("ref", out var baseRefEl)
            ? baseRefEl.GetString() ?? ""
            : "";
        var author = pr.TryGetProperty("user", out var userEl)
                     && userEl.TryGetProperty("login", out var loginEl)
            ? loginEl.GetString() ?? ""
            : pr.TryGetProperty("user", out var userEl2)
              && userEl2.TryGetProperty("username", out var usernameEl)
                ? usernameEl.GetString() ?? ""
                : "";
        var url = pr.TryGetProperty("html_url", out var urlEl) ? urlEl.GetString() : null;

        var action = MapAction(actionStr, pr);
        if (action is null)
        {
            return new List<GitWebhookEvent>();
        }

        return new List<GitWebhookEvent>
        {
            new PullRequestEvent
            {
                OccurredAt = DateTimeOffset.UtcNow,
                ProviderId = ProviderId,
                Repository = repo,
                Action = action.Value,
                Number = prNumber,
                Title = title,
                Body = prBody,
                SourceBranch = sourceBranch,
                TargetBranch = targetBranch,
                Author = author,
                Url = url
            }
        };
    }

    private static PullRequestAction? MapAction(string action, JsonElement pr) => action switch
    {
        "opened" => PullRequestAction.Opened,
        "edited" or "synchronized" or "reopened" => PullRequestAction.Updated,
        "closed" => pr.TryGetProperty("pull_request", out _) // Forgejo wraps oddly in some payloads
            && pr.TryGetProperty("merged", out var mergedEl)
            && mergedEl.GetBoolean()
                ? PullRequestAction.Merged
                : (pr.TryGetProperty("merged", out var mergedEl2) && mergedEl2.GetBoolean()
                    ? PullRequestAction.Merged
                    : PullRequestAction.Closed),
        _ => null
    };
}
