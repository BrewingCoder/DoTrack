using System.Text.Json.Serialization;
using DoTrack.Api.AcceptanceCriteria;
using DoTrack.Api.Auditing;
using DoTrack.Api.Bootstrap;
using DoTrack.Api.Comments;
using DoTrack.Api.Configuration;
using DoTrack.Api.Middleware;
using DoTrack.Api.Milestones;
using DoTrack.Api.SavedQueries;
using DoTrack.Api.Sprints;
using DoTrack.Api.Time;
using DoTrack.Api.Webhooks;
using DoTrack.Api.WorkItems;
using DoTrack.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddConfiguredDatabase(builder.Configuration);
builder.Services.AddSingleton<DoTrack.GitProviders.GitHub.GitHubAdapter>();
builder.Services.AddSingleton<DoTrack.GitProviders.Gitea.GiteaAdapter>();
builder.Services.AddSingleton<DoTrack.GitProviders.Bitbucket.BitbucketAdapter>();

// Outbox + n8n outbound delivery. Wired only when n8n is configured.
var n8nUrl = builder.Configuration["Automation:N8n:WebhookUrl"];
if (!string.IsNullOrEmpty(n8nUrl))
{
    builder.Services.AddSingleton(new DoTrack.Automation.N8n.N8nAutomationProviderOptions
    {
        WebhookUrl = n8nUrl,
        Secret = builder.Configuration["Automation:N8n:Secret"]
    });
    builder.Services.AddHttpClient<DoTrack.Automation.N8n.N8nAutomationProvider>();
    builder.Services.AddSingleton<DoTrack.Automation.Abstractions.IAutomationProvider>(sp =>
        sp.GetRequiredService<DoTrack.Automation.N8n.N8nAutomationProvider>());
    builder.Services.AddSingleton<DoTrack.Workers.OutboxDispatcherOptions>(_ => new());
    builder.Services.AddHostedService<DoTrack.Workers.OutboxDispatcherService>();
}

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseStatusCodePages();
app.UseExceptionHandler();
app.UseMiddleware<AuditContextMiddleware>();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.MapGet("/healthz/db", async (DoTrackDbContext db, CancellationToken ct) =>
{
    var canConnect = await db.Database.CanConnectAsync(ct);
    return canConnect
        ? Results.Ok(new { status = "ok", provider = db.Database.ProviderName })
        : Results.Problem("Cannot connect to database", statusCode: 503);
});

app.MapBootstrapEndpoints();
app.MapWorkItemEndpoints();
app.MapWorkItemLinkEndpoints();
app.MapWatcherEndpoints();
app.MapCommentEndpoints();
app.MapTimeEntryEndpoints();
app.MapAcceptanceCriteriaEndpoints();
app.MapSprintEndpoints();
app.MapMilestoneEndpoints();
app.MapSavedQueryEndpoints();
app.MapAuditEndpoints();
app.MapGitHubWebhookEndpoint();
app.MapGiteaWebhookEndpoint();
app.MapBitbucketWebhookEndpoint();

app.Run();

public partial class Program;
