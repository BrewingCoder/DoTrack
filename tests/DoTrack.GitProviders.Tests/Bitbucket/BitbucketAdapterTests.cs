using System.Security.Cryptography;
using System.Text;
using DoTrack.GitProviders.Abstractions;
using DoTrack.GitProviders.Bitbucket;
using Shouldly;

namespace DoTrack.GitProviders.Tests.Bitbucket;

public class BitbucketAdapterTests
{
    private const string Secret = "topsecret";
    private readonly BitbucketAdapter _adapter = new();

    private static string ComputeSignature(string body, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return "sha256=" + Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes(body)));
    }

    [Fact]
    public void VerifySignature_CorrectHmac_ReturnsTrue()
    {
        var body = "{\"x\":1}";
        var sig = ComputeSignature(body, Secret);
        var req = new WebhookRequest("repo:push", new Dictionary<string, string>
        {
            ["X-Hub-Signature"] = sig
        }, body);
        _adapter.VerifySignature(req, Secret).ShouldBeTrue();
    }

    [Fact]
    public void VerifySignature_NonSha256Prefix_ReturnsFalse()
    {
        var req = new WebhookRequest("repo:push", new Dictionary<string, string>
        {
            ["X-Hub-Signature"] = "sha1=abcdef"
        }, "{}");
        _adapter.VerifySignature(req, Secret).ShouldBeFalse();
    }

    [Fact]
    public async Task ParsePush_SingleChange_ExtractsBranchAndCommits()
    {
        var body = """
        {
          "repository": { "full_name": "scott/dotrack" },
          "push": {
            "changes": [
              {
                "new": { "type": "branch", "name": "feature/login" },
                "commits": [
                  {
                    "hash": "deadbeef",
                    "message": "PROJ-1 fix",
                    "author": { "raw": "Scott Singleton <scott@gscottsingleton.com>" },
                    "date": "2026-05-04T01:00:00Z",
                    "links": { "html": { "href": "https://bitbucket.org/scott/dotrack/commits/deadbeef" } }
                  }
                ]
              }
            ]
          }
        }
        """;
        var req = new WebhookRequest("repo:push", new Dictionary<string, string>(), body);
        var events = await _adapter.ParseWebhookAsync(req, default);

        var push = events.Single().ShouldBeOfType<CommitPushed>();
        push.Branch.ShouldBe("feature/login");
        push.Commits.Single().Sha.ShouldBe("deadbeef");
        push.Commits[0].AuthorName.ShouldBe("Scott Singleton");
        push.Commits[0].AuthorEmail.ShouldBe("scott@gscottsingleton.com");
    }

    [Fact]
    public async Task ParsePullRequestCreated_MapsToOpened()
    {
        var body = """
        {
          "repository": { "full_name": "scott/dotrack" },
          "pullrequest": {
            "id": 7,
            "title": "Add login",
            "description": "Resolves PROJ-42",
            "source": { "branch": { "name": "feature/login" } },
            "destination": { "branch": { "name": "main" } },
            "author": { "username": "scott" },
            "state": "OPEN",
            "links": { "html": { "href": "https://bitbucket.org/scott/dotrack/pull-requests/7" } }
          }
        }
        """;
        var req = new WebhookRequest("pullrequest:created", new Dictionary<string, string>(), body);
        var events = await _adapter.ParseWebhookAsync(req, default);

        var pr = events.Single().ShouldBeOfType<PullRequestEvent>();
        pr.Action.ShouldBe(PullRequestAction.Opened);
        pr.Number.ShouldBe(7);
        pr.SourceBranch.ShouldBe("feature/login");
        pr.TargetBranch.ShouldBe("main");
        pr.Author.ShouldBe("scott");
    }

    [Fact]
    public async Task ParsePullRequestFulfilled_MapsToMerged()
    {
        var body = """
        {
          "repository": { "full_name": "x/y" },
          "pullrequest": {
            "id": 1,
            "title": "x",
            "source": { "branch": { "name": "f" } },
            "destination": { "branch": { "name": "main" } },
            "author": { "display_name": "S" },
            "state": "MERGED"
          }
        }
        """;
        var req = new WebhookRequest("pullrequest:fulfilled", new Dictionary<string, string>(), body);
        var events = await _adapter.ParseWebhookAsync(req, default);
        ((PullRequestEvent)events.Single()).Action.ShouldBe(PullRequestAction.Merged);
    }

    [Fact]
    public async Task ParsePullRequestRejected_MapsToClosed()
    {
        var body = """
        {
          "repository": { "full_name": "x/y" },
          "pullrequest": {
            "id": 1,
            "title": "x",
            "source": { "branch": { "name": "f" } },
            "destination": { "branch": { "name": "main" } },
            "author": { "display_name": "S" },
            "state": "DECLINED"
          }
        }
        """;
        var req = new WebhookRequest("pullrequest:rejected", new Dictionary<string, string>(), body);
        var events = await _adapter.ParseWebhookAsync(req, default);
        ((PullRequestEvent)events.Single()).Action.ShouldBe(PullRequestAction.Closed);
    }

    [Fact]
    public async Task ParseUnknownEvent_ReturnsEmpty()
    {
        var req = new WebhookRequest("issue:created", new Dictionary<string, string>(), "{}");
        var events = await _adapter.ParseWebhookAsync(req, default);
        events.ShouldBeEmpty();
    }
}
