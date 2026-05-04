using DoTrack.Domain.Identity;
using DoTrack.Domain.Workspaces;
using DoTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace DoTrack.Integration.Tests;

public sealed class DoTrackApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("dotrack_integration")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DoTrackDbContext>();
        await db.Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _container.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "postgres",
                ["Database:ConnectionString"] = _container.GetConnectionString()
            });
        });
    }

    public async Task ResetDataAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DoTrackDbContext>();
        await db.AuditLogs.ExecuteDeleteAsync();
        await db.MilestoneScope.ExecuteDeleteAsync();
        await db.Milestones.ExecuteDeleteAsync();
        await db.AcceptanceCriteria.ExecuteDeleteAsync();
        await db.Comments.ExecuteDeleteAsync();
        await db.TimeEntries.ExecuteDeleteAsync();
        await db.WorkItemHierarchies.ExecuteDeleteAsync();
        await db.WorkItemLinks.ExecuteDeleteAsync();
        await db.WorkItems.ExecuteDeleteAsync();
        await db.Sprints.ExecuteDeleteAsync();
        await db.Projects.ExecuteDeleteAsync();
        await db.Workspaces.ExecuteDeleteAsync();
        await db.Users.ExecuteDeleteAsync();
    }

    public async Task<(Workspace Workspace, Project Project, User Reporter)> SeedAsync(
        string workspaceSlug = "acme",
        string projectKey = "PROJ")
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DoTrackDbContext>();
        var now = DateTimeOffset.UtcNow;
        var workspace = new Workspace(WorkspaceId.New(), "Acme Inc", workspaceSlug, now);
        var project = new Project(ProjectId.New(), workspace.Id, projectKey, "Test Project", null, now);
        var reporter = new User(UserId.New(), $"reporter-{Guid.NewGuid():N}@test.com", "Test Reporter", now);
        db.Workspaces.Add(workspace);
        db.Projects.Add(project);
        db.Users.Add(reporter);
        await db.SaveChangesAsync();
        return (workspace, project, reporter);
    }
}
