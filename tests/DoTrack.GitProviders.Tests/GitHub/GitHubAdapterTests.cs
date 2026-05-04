using System.Security.Cryptography;
using System.Text;
using DoTrack.GitProviders.Abstractions;
using DoTrack.GitProviders.GitHub;
using Shouldly;

namespace DoTrack.GitProviders.Tests.GitHub;

public class GitHubAdapterTests
{
    private const string Secret = "topsecret";
    private readonly GitHubAdapter _adapter = new();

    private static string ComputeSignature(string body, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        return "sha256=" + Convert.ToHexStringLower(hash);
    }

    [Fact]
    public void VerifySignature_CorrectHmac_ReturnsTrue()
    {
        var body = "{\"some\":\"payload\"}";
        var sig = ComputeSignature(body, Secret);
        var req = new WebhookRequest("push", new Dictionary<string, string>
        {
            ["X-Hub-Signature-256"] = sig
        }, body);

        _adapter.VerifySignature(req, Secret).ShouldBeTrue();
    }

    [Fact]
    public void VerifySignature_WrongSignature_ReturnsFalse()
    {
        var body = "{\"some\":\"payload\"}";
        var sig = ComputeSignature(body, "differentsecret");
        var req = new WebhookRequest("push", new Dictionary<string, string>
        {
            ["X-Hub-Signature-256"] = sig
        }, body);

        _adapter.VerifySignature(req, Secret).ShouldBeFalse();
    }

    [Fact]
    public void VerifySignature_BodyTampered_ReturnsFalse()
    {
        var sig = ComputeSignature("{\"original\":1}", Secret);
        var req = new WebhookRequest("push", new Dictionary<string, string>
        {
            ["X-Hub-Signature-256"] = sig
        }, "{\"tampered\":1}");

        _adapter.VerifySignature(req, Secret).ShouldBeFalse();
    }

    [Fact]
    public void VerifySignature_MissingHeader_ReturnsFalse()
    {
        var req = new WebhookRequest("push", new Dictionary<string, string>(), "{}");
        _adapter.VerifySignature(req, Secret).ShouldBeFalse();
    }

    [Fact]
    public void VerifySignature_LowercaseHeaderKey_AcceptedToo()
    {
        var body = "{\"x\":1}";
        var sig = ComputeSignature(body, Secret);
        var req = new WebhookRequest("push", new Dictionary<string, string>
        {
            ["x-hub-signature-256"] = sig
        }, body);
        _adapter.VerifySignature(req, Secret).ShouldBeTrue();
    }

    [Fact]
    public void VerifySignature_NonSha256Prefix_ReturnsFalse()
    {
        var req = new WebhookRequest("push", new Dictionary<string, string>
        {
            ["X-Hub-Signature-256"] = "sha1=abcdef"
        }, "{}");
        _adapter.VerifySignature(req, Secret).ShouldBeFalse();
    }

    [Fact]
    public async Task ParseWebhookAsync_PushEvent_ExtractsRepoBranchCommits()
    {
        var body = """
        {
          "ref": "refs/heads/feature/login",
          "repository": { "full_name": "BrewingCoder/DoTrack" },
          "commits": [
            {
              "id": "abc123",
              "message": "PROJ-42 #fixed: login redirect",
              "author": { "name": "Scott", "email": "scott@gscottsingleton.com" },
              "timestamp": "2026-05-04T01:23:45Z",
              "url": "https://github.com/BrewingCoder/DoTrack/commit/abc123"
            }
          ]
        }
        """;
        var req = new WebhookRequest("push", new Dictionary<string, string>(), body);
        var events = await _adapter.ParseWebhookAsync(req, default);

        events.Count.ShouldBe(1);
        var push = events[0].ShouldBeOfType<CommitPushed>();
        push.Repository.ShouldBe("BrewingCoder/DoTrack");
        push.Branch.ShouldBe("feature/login");
        push.ProviderId.ShouldBe("github");
        push.Commits.Count.ShouldBe(1);
        push.Commits[0].Sha.ShouldBe("abc123");
        push.Commits[0].Message.ShouldBe("PROJ-42 #fixed: login redirect");
        push.Commits[0].AuthorEmail.ShouldBe("scott@gscottsingleton.com");
    }

    [Theory]
    [InlineData("opened", PullRequestAction.Opened)]
    [InlineData("edited", PullRequestAction.Updated)]
    [InlineData("synchronize", PullRequestAction.Updated)]
    [InlineData("ready_for_review", PullRequestAction.Updated)]
    [InlineData("reopened", PullRequestAction.Updated)]
    public async Task ParseWebhookAsync_PullRequestActions_MappedCorrectly(string action, PullRequestAction expected)
    {
        var body = $$"""
        {
          "action": "{{action}}",
          "number": 7,
          "pull_request": {
            "title": "PROJ-42 fix login",
            "body": "Resolves PROJ-42",
            "head": { "ref": "feature/login" },
            "base": { "ref": "main" },
            "user": { "login": "BrewingCoder" },
            "html_url": "https://github.com/BrewingCoder/DoTrack/pull/7",
            "merged": false
          },
          "repository": { "full_name": "BrewingCoder/DoTrack" }
        }
        """;
        var req = new WebhookRequest("pull_request", new Dictionary<string, string>(), body);
        var events = await _adapter.ParseWebhookAsync(req, default);

        events.Count.ShouldBe(1);
        var pr = events[0].ShouldBeOfType<PullRequestEvent>();
        pr.Action.ShouldBe(expected);
        pr.Number.ShouldBe(7);
        pr.Title.ShouldBe("PROJ-42 fix login");
        pr.SourceBranch.ShouldBe("feature/login");
        pr.TargetBranch.ShouldBe("main");
        pr.Author.ShouldBe("BrewingCoder");
    }

    [Fact]
    public async Task ParseWebhookAsync_ClosedAndMerged_MapsToMerged()
    {
        var body = """
        {
          "action": "closed",
          "number": 7,
          "pull_request": {
            "title": "x",
            "head": { "ref": "f" },
            "base": { "ref": "main" },
            "user": { "login": "x" },
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
    public async Task ParseWebhookAsync_ClosedNotMerged_MapsToClosed()
    {
        var body = """
        {
          "action": "closed",
          "number": 7,
          "pull_request": {
            "title": "x",
            "head": { "ref": "f" },
            "base": { "ref": "main" },
            "user": { "login": "x" },
            "merged": false
          },
          "repository": { "full_name": "x/y" }
        }
        """;
        var req = new WebhookRequest("pull_request", new Dictionary<string, string>(), body);
        var events = await _adapter.ParseWebhookAsync(req, default);

        ((PullRequestEvent)events.Single()).Action.ShouldBe(PullRequestAction.Closed);
    }

    [Fact]
    public async Task ParseWebhookAsync_UnknownAction_ReturnsEmpty()
    {
        var body = """
        {
          "action": "labeled",
          "number": 1,
          "pull_request": {
            "title": "x",
            "head": { "ref": "f" },
            "base": { "ref": "main" },
            "user": { "login": "x" },
            "merged": false
          },
          "repository": { "full_name": "x/y" }
        }
        """;
        var req = new WebhookRequest("pull_request", new Dictionary<string, string>(), body);
        var events = await _adapter.ParseWebhookAsync(req, default);
        events.ShouldBeEmpty();
    }

    [Fact]
    public async Task ParseWebhookAsync_UnknownEventType_ReturnsEmpty()
    {
        var req = new WebhookRequest("ping", new Dictionary<string, string>(), "{}");
        var events = await _adapter.ParseWebhookAsync(req, default);
        events.ShouldBeEmpty();
    }
}
