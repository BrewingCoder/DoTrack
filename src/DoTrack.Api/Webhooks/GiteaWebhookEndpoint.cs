using DoTrack.Application.Webhooks;
using DoTrack.GitProviders.Abstractions;
using DoTrack.GitProviders.Gitea;
using Microsoft.Extensions.Logging;

namespace DoTrack.Api.Webhooks;

public static class GiteaWebhookEndpoint
{
    public static IEndpointRouteBuilder MapGiteaWebhookEndpoint(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/v1/webhooks/gitea", HandleAsync).WithTags("Webhooks");
        return routes;
    }

    private static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        GiteaAdapter adapter,
        IConfiguration configuration,
        IWebhookEventDispatcher dispatcher,
        ILogger<GiteaAdapter> logger,
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
        var eventType = headers.TryGetValue("X-Gitea-Event", out var et)
            ? et
            : headers.TryGetValue("X-Gitea-Event-Type", out var et2) ? et2 : string.Empty;

        var webhook = new WebhookRequest(eventType, headers, body);

        var secret = configuration["Webhooks:Gitea:Secret"];
        if (!string.IsNullOrEmpty(secret))
        {
            if (!adapter.VerifySignature(webhook, secret))
            {
                logger.LogWarning("Rejected Gitea webhook for invalid signature on event {EventType}", eventType);
                return Results.Unauthorized();
            }
        }

        var events = await adapter.ParseWebhookAsync(webhook, cancellationToken);

        foreach (var evt in events)
        {
            logger.LogInformation(
                "Gitea webhook event: {ProviderId} {EventType} repo={Repo} occurredAt={OccurredAt}",
                evt.ProviderId, evt.GetType().Name, evt.Repository, evt.OccurredAt);
        }

        await dispatcher.DispatchAsync(events, cancellationToken);

        return Results.Accepted();
    }
}
