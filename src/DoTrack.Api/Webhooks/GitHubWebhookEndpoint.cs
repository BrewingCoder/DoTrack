using DoTrack.GitProviders.Abstractions;
using DoTrack.GitProviders.GitHub;
using Microsoft.Extensions.Logging;

namespace DoTrack.Api.Webhooks;

public static class GitHubWebhookEndpoint
{
    public static IEndpointRouteBuilder MapGitHubWebhookEndpoint(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/v1/webhooks/github", HandleAsync).WithTags("Webhooks");
        return routes;
    }

    private static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        GitHubAdapter adapter,
        IConfiguration configuration,
        ILogger<GitHubAdapter> logger,
        CancellationToken cancellationToken)
    {
        httpContext.Request.EnableBuffering();
        string body;
        using (var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true))
        {
            body = await reader.ReadToEndAsync(cancellationToken);
        }
        httpContext.Request.Body.Position = 0;

        var headers = httpContext.Request.Headers
            .ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        var eventType = headers.TryGetValue("X-GitHub-Event", out var et) ? et : string.Empty;

        var webhook = new WebhookRequest(eventType, headers, body);

        var secret = configuration["Webhooks:GitHub:Secret"];
        if (!string.IsNullOrEmpty(secret))
        {
            if (!adapter.VerifySignature(webhook, secret))
            {
                logger.LogWarning("Rejected GitHub webhook for invalid signature on event {EventType}", eventType);
                return Results.Unauthorized();
            }
        }

        var events = await adapter.ParseWebhookAsync(webhook, cancellationToken);

        // v0: log + acknowledge. Full event dispatch (audit, comment-on-linked-issue,
        // status transition via smart-commit commands) ships once we wire an outbox/dispatcher.
        foreach (var evt in events)
        {
            logger.LogInformation(
                "GitHub webhook event: {ProviderId} {EventType} repo={Repo} occurredAt={OccurredAt}",
                evt.ProviderId, evt.GetType().Name, evt.Repository, evt.OccurredAt);
        }

        return Results.Accepted();
    }
}
