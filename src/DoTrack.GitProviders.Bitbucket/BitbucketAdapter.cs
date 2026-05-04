using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DoTrack.GitProviders.Abstractions;

namespace DoTrack.GitProviders.Bitbucket;

/// <summary>
/// IGitProviderAdapter for Bitbucket Cloud. Bitbucket Server / Data Center
/// uses a different API surface and is deferred to v1.x.
///
/// Webhook signature uses X-Hub-Signature with "sha256=&lt;hex&gt;" format
/// (matches GitHub's convention). Bitbucket only signs payloads when a
/// secret is configured per-webhook in the Bitbucket UI.
///
/// Event-key header X-Event-Key carries values like "repo:push" and
/// "pullrequest:created". Push payloads nest commits under push.changes[].
/// PR payloads carry state directly on the pull_request object.
/// </summary>
public sealed class BitbucketAdapter : IGitProviderAdapter
{
    public string ProviderId => "bitbucket";
    public string DisplayName => "Bitbucket Cloud";

    public bool VerifySignature(WebhookRequest request, string secret)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(secret);

        if (!request.Headers.TryGetValue("X-Hub-Signature", out var headerValue)
            && !request.Headers.TryGetValue("x-hub-signature", out headerValue))
        {
            return false;
        }

        const string prefix = "sha256=";
        if (!headerValue.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var expectedHex = headerValue[prefix.Length..];
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(request.Body);

        using var hmac = new HMACSHA256(keyBytes);
        var computed = Convert.ToHexStringLower(hmac.ComputeHash(bodyBytes));

        if (expectedHex.Length != computed.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expectedHex.ToLowerInvariant()),
            Encoding.ASCII.GetBytes(computed));
    }

    public Task<IReadOnlyList<GitWebhookEvent>> ParseWebhookAsync(
        WebhookRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var eventType = request.EventType?.ToLowerInvariant();
        IReadOnlyList<GitWebhookEvent> events = eventType switch
        {
            "repo:push" => ParsePush(request.Body),
            "pullrequest:created" => ParsePullRequest(request.Body, PullRequestAction.Opened),
            "pullrequest:updated" => ParsePullRequest(request.Body, PullRequestAction.Updated),
            "pullrequest:fulfilled" => ParsePullRequest(request.Body, PullRequestAction.Merged),
            "pullrequest:rejected" => ParsePullRequest(request.Body, PullRequestAction.Closed),
            _ => Array.Empty<GitWebhookEvent>()
        };
        return Task.FromResult(events);
    }

    private List<GitWebhookEvent> ParsePush(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var repo = root.TryGetProperty("repository", out var repoEl)
                   && repoEl.TryGetProperty("full_name", out var fullName)
            ? fullName.GetString() ?? ""
            : "";

        var events = new List<GitWebhookEvent>();
        if (!root.TryGetProperty("push", out var pushEl)
            || !pushEl.TryGetProperty("changes", out var changesEl)
            || changesEl.ValueKind != JsonValueKind.Array)
        {
            return events;
        }

        foreach (var change in changesEl.EnumerateArray())
        {
            string branch;
            if (change.TryGetProperty("new", out var newEl)
                && newEl.ValueKind != JsonValueKind.Null
                && newEl.TryGetProperty("name", out var nameEl))
            {
                branch = nameEl.GetString() ?? "";
            }
            else
            {
                continue;
            }

            var commits = new List<GitCommit>();
            if (change.TryGetProperty("commits", out var commitsEl) && commitsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in commitsEl.EnumerateArray())
                {
                    var sha = c.TryGetProperty("hash", out var hashEl) ? hashEl.GetString() ?? "" : "";
                    var message = c.TryGetProperty("message", out var msgEl) ? msgEl.GetString() ?? "" : "";

                    string authorName = "";
                    string authorEmail = "";
                    if (c.TryGetProperty("author", out var authEl)
                        && authEl.TryGetProperty("raw", out var rawEl)
                        && rawEl.ValueKind == JsonValueKind.String)
                    {
                        // Bitbucket "author.raw" is "Name <email@example.com>"
                        var raw = rawEl.GetString() ?? "";
                        var lt = raw.IndexOf('<');
                        var gt = raw.IndexOf('>');
                        if (lt > 0 && gt > lt)
                        {
                            authorName = raw[..lt].Trim();
                            authorEmail = raw.Substring(lt + 1, gt - lt - 1).Trim();
                        }
                        else
                        {
                            authorName = raw;
                        }
                    }

                    var ts = c.TryGetProperty("date", out var dateEl)
                             && dateEl.ValueKind == JsonValueKind.String
                             && DateTimeOffset.TryParse(dateEl.GetString(), out var parsed)
                        ? parsed
                        : DateTimeOffset.UtcNow;

                    string? url = null;
                    if (c.TryGetProperty("links", out var linksEl)
                        && linksEl.TryGetProperty("html", out var htmlEl)
                        && htmlEl.TryGetProperty("href", out var hrefEl))
                    {
                        url = hrefEl.GetString();
                    }

                    commits.Add(new GitCommit(sha, message, authorName, authorEmail, ts, url));
                }
            }

            events.Add(new CommitPushed
            {
                OccurredAt = DateTimeOffset.UtcNow,
                ProviderId = ProviderId,
                Repository = repo,
                Branch = branch,
                Commits = commits
            });
        }

        return events;
    }

    private List<GitWebhookEvent> ParsePullRequest(string body, PullRequestAction action)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var repo = root.TryGetProperty("repository", out var repoEl)
                   && repoEl.TryGetProperty("full_name", out var fullName)
            ? fullName.GetString() ?? ""
            : "";

        if (!root.TryGetProperty("pullrequest", out var pr))
        {
            return new List<GitWebhookEvent>();
        }

        var number = pr.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0;
        var title = pr.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? "" : "";
        var prBody = pr.TryGetProperty("description", out var descEl) ? descEl.GetString() : null;
        var sourceBranch = pr.TryGetProperty("source", out var srcEl)
                           && srcEl.TryGetProperty("branch", out var srcBranchEl)
                           && srcBranchEl.TryGetProperty("name", out var srcNameEl)
            ? srcNameEl.GetString() ?? ""
            : "";
        var targetBranch = pr.TryGetProperty("destination", out var dstEl)
                           && dstEl.TryGetProperty("branch", out var dstBranchEl)
                           && dstBranchEl.TryGetProperty("name", out var dstNameEl)
            ? dstNameEl.GetString() ?? ""
            : "";
        var author = pr.TryGetProperty("author", out var authorEl)
                     && (authorEl.TryGetProperty("username", out var unEl)
                         || authorEl.TryGetProperty("display_name", out unEl)
                         || authorEl.TryGetProperty("nickname", out unEl))
            ? unEl.GetString() ?? ""
            : "";

        string? url = null;
        if (pr.TryGetProperty("links", out var linksEl)
            && linksEl.TryGetProperty("html", out var htmlEl)
            && htmlEl.TryGetProperty("href", out var hrefEl))
        {
            url = hrefEl.GetString();
        }

        return new List<GitWebhookEvent>
        {
            new PullRequestEvent
            {
                OccurredAt = DateTimeOffset.UtcNow,
                ProviderId = ProviderId,
                Repository = repo,
                Action = action,
                Number = number,
                Title = title,
                Body = prBody,
                SourceBranch = sourceBranch,
                TargetBranch = targetBranch,
                Author = author,
                Url = url
            }
        };
    }
}
