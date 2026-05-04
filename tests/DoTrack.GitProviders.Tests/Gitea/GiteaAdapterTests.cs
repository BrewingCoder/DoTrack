using System.Security.Cryptography;
using System.Text;
using DoTrack.GitProviders.Abstractions;
using DoTrack.GitProviders.Gitea;
using Shouldly;

namespace DoTrack.GitProviders.Tests.Gitea;

public class GiteaAdapterTests
{
    private const string Secret = "topsecret";
    private readonly GiteaAdapter _adapter = new();

    private static string ComputeSignature(string body, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes(body)));
    }

    [Fact]
    public void VerifySignature_CorrectHmac_ReturnsTrue()
    {
        var body = "{\"x\":1}";
        var sig = ComputeSignature(body, Secret);
        var req = new WebhookRequest("push", new Dictionary<string, string>
        {
            ["X-Gitea-Signature"] = sig
        }, body);
        _adapter.VerifySignature(req, Secret).ShouldBeTrue();
    }

    [Fact]
    public void VerifySignature_BodyTampered_ReturnsFalse()
    {
        var sig = ComputeSignature("{\"orig\":1}", Secret);
        var req = new WebhookRequest("push", new Dictionary<string, string>
        {
            ["X-Gitea-Signature"] = sig
        }, "{\"tamp\":1}");
        _adapter.VerifySignature(req, Secret).ShouldBeFalse();
    }

    [Fact]
    public void VerifySignature_MissingHeader_ReturnsFalse()
    {
        var req = new WebhookRequest("push", new Dictionary<string, string>(), "{}");
        _adapter.VerifySignature(req, Secret).ShouldBeFalse();
    }

    [Fact]
    public async Task ParseWebhookAsync_PushEvent_ExtractsBranchAndCommits()
    {
        var body = """
        {
          "ref": "refs/heads/main",
          "repository": { "full_name": "scott/dotrack" },
          "commits": [
            {
              "id": "deadbeef",
              "message": "PROJ-1 commit",
              "author": { "name": "S", "email": "s@e" },
              "timestamp": "2026-05-04T01:00:00Z",
              "url": "https://gitea.local/c/deadbeef"
            }
          ]
        }
        """;
        var req = new WebhookRequest("push", new Dictionary<string, string>(), body);
        var events = await _adapter.ParseWebhookAsync(req, default);

        var push = events.Single().ShouldBeOfType<CommitPushed>();
        push.Branch.ShouldBe("main");
        push.Repository.ShouldBe("scott/dotrack");
        push.ProviderId.ShouldBe("gitea");
        push.Commits.Single().Sha.ShouldBe("deadbeef");
    }

    [Fact]
    public async Task ParseWebhookAsync_PullRequestOpened_Maps()
    {
        var body = """
        {
          "action": "opened",
          "number": 9,
          "pull_request": {
            "title": "Title",
            "body": "Body",
            "head": { "ref": "feature" },
            "base": { "ref": "main" },
            "user": { "username": "scott" },
            "html_url": "https://gitea.local/p/9",
            "merged": false
          },
          "repository": { "full_name": "scott/dotrack" }
        }
        """;
        var req = new WebhookRequest("pull_request", new Dictionary<string, string>(), body);
        var events = await _adapter.ParseWebhookAsync(req, default);
        var pr = events.Single().ShouldBeOfType<PullRequestEvent>();
        pr.Action.ShouldBe(PullRequestAction.Opened);
        pr.Author.ShouldBe("scott");
        pr.SourceBranch.ShouldBe("feature");
        pr.TargetBranch.ShouldBe("main");
    }

    [Fact]
    public async Task ParseWebhookAsync_PullRequestClosedMerged_MapsToMerged()
    {
        var body = """
        {
          "action": "closed",
          "number": 9,
          "pull_request": {
            "title": "x",
            "head": { "ref": "f" },
            "base": { "ref": "main" },
            "user": { "username": "s" },
            "merged": true
          },
          "repository": { "full_name": "x/y" }
        }
        """;
        var req = new WebhookRequest("pull_request", new Dictionary<string, string>(), body);
        var events = await _adapter.ParseWebhookAsync(req, default);
        ((PullRequestEvent)events.Single()).Action.ShouldBe(PullRequestAction.Merged);
    }

    [Fact]
    public async Task ParseWebhookAsync_UnknownEvent_ReturnsEmpty()
    {
        var req = new WebhookRequest("issue_comment", new Dictionary<string, string>(), "{}");
        var events = await _adapter.ParseWebhookAsync(req, default);
        events.ShouldBeEmpty();
    }
}
