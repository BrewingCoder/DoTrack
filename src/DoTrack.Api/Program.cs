using System.Text.Json.Serialization;
using DoTrack.Api.AcceptanceCriteria;
using DoTrack.Api.Auditing;
using DoTrack.Api.Bootstrap;
using DoTrack.Api.Comments;
using DoTrack.Api.Configuration;
using DoTrack.Api.Middleware;
using DoTrack.Api.Milestones;
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
app.MapCommentEndpoints();
app.MapTimeEntryEndpoints();
app.MapAcceptanceCriteriaEndpoints();
app.MapSprintEndpoints();
app.MapMilestoneEndpoints();
app.MapAuditEndpoints();
app.MapGitHubWebhookEndpoint();

app.Run();

public partial class Program;
