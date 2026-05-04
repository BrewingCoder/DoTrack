using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DoTrack.Automation.Abstractions;

namespace DoTrack.Automation.N8n;

public sealed class N8nAutomationProviderOptions
{
    public string? WebhookUrl { get; set; }
    public string? Secret { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}

public sealed class N8nAutomationProvider(HttpClient httpClient, N8nAutomationProviderOptions options) : IAutomationProvider
{
    public string ProviderId => "n8n";
    public string DisplayName => "n8n";

    public async Task DeliverEventAsync(AutomationEvent evt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(evt);

        if (string.IsNullOrEmpty(options.WebhookUrl))
        {
            throw new InvalidOperationException("N8n webhook URL is not configured.");
        }

        var json = JsonSerializer.Serialize(evt);
        using var request = new HttpRequestMessage(HttpMethod.Post, options.WebhookUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrEmpty(options.Secret))
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(options.Secret));
            var sig = Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes(json)));
            request.Headers.Add("X-DoTrack-Signature-256", $"sha256={sig}");
        }
        request.Headers.Add("X-DoTrack-Event-Id", evt.EventId.ToString());
        request.Headers.Add("X-DoTrack-Event-Type", evt.EventType);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(options.Timeout);

        var response = await httpClient.SendAsync(request, cts.Token);
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(options.WebhookUrl))
        {
            return false;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, options.WebhookUrl);
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            using var response = await httpClient.SendAsync(request, cts.Token);
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed;
        }
        catch (HttpRequestException) { return false; }
        catch (TaskCanceledException) { return false; }
    }
}
