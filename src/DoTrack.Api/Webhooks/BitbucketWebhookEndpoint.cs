using DoTrack.Application.Webhooks;
using DoTrack.GitProviders.Abstractions;
using DoTrack.GitProviders.Bitbucket;
using Microsoft.Extensions.Logging;

namespace DoTrack.Api.Webhooks;

public static class BitbucketWebhookEndpoint
{
    public static IEndpointRouteBuilder MapBitbucketWebhookEndpoint(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/v1/webhooks/bitbucket", HandleAsync).WithTags("Webhooks");
        return routes;
    }

    private static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        BitbucketAdapter adapter,
        IConfiguration configuration,
        IWebhookEventDispatcher dispatcher,
        ILogger<BitbucketAdapter> logger,
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
        var eventType = headers.TryGetValue("X-Event-Key", out var et) ? et : string.Empty;

        var webhook = new WebhookRequest(eventType, headers, body);

        var secret = configuration["Webhooks:Bitbucket:Secret"];
        if (!string.IsNullOrEmpty(secret))
        {
            if (!adapter.VerifySignature(webhook, secret))
            {
                logger.LogWarning("Rejected Bitbucket webhook for invalid signature on event {EventType}", eventType);
                return Results.Unauthorized();
            }
        }

        var events = await adapter.ParseWebhookAsync(webhook, cancellationToken);

        foreach (var evt in events)
        {
            logger.LogInformation(
                "Bitbucket webhook event: {ProviderId} {EventType} repo={Repo} occurredAt={OccurredAt}",
                evt.ProviderId, evt.GetType().Name, evt.Repository, evt.OccurredAt);
        }

        await dispatcher.DispatchAsync(events, cancellationToken);

        return Results.Accepted();
    }
}
