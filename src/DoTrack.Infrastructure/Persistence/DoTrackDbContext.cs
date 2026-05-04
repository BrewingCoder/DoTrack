using DoTrack.Domain.Auditing;
using DoTrack.Domain.Comments;
using DoTrack.Domain.Identity;
using DoTrack.Domain.Time;
using DoTrack.Domain.WorkItems;
using DoTrack.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DoTrack.Infrastructure.Persistence;

public class DoTrackDbContext(DbContextOptions<DoTrackDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<WorkItemHierarchy> WorkItemHierarchies => Set<WorkItemHierarchy>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<AcceptanceCriterion> AcceptanceCriteria => Set<AcceptanceCriterion>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DoTrackDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<UserId>().HaveConversion<UserIdConverter>();
        configurationBuilder.Properties<WorkspaceId>().HaveConversion<WorkspaceIdConverter>();
        configurationBuilder.Properties<ProjectId>().HaveConversion<ProjectIdConverter>();
        configurationBuilder.Properties<WorkItemId>().HaveConversion<WorkItemIdConverter>();
        configurationBuilder.Properties<TimeEntryId>().HaveConversion<TimeEntryIdConverter>();
        configurationBuilder.Properties<CommentId>().HaveConversion<CommentIdConverter>();
        configurationBuilder.Properties<AcceptanceCriterionId>().HaveConversion<AcceptanceCriterionIdConverter>();
        configurationBuilder.Properties<AuditLogId>().HaveConversion<AuditLogIdConverter>();
    }

    private sealed class UserIdConverter() : ValueConverter<UserId, Guid>(id => id.Value, v => new UserId(v));
    private sealed class WorkspaceIdConverter() : ValueConverter<WorkspaceId, Guid>(id => id.Value, v => new WorkspaceId(v));
    private sealed class ProjectIdConverter() : ValueConverter<ProjectId, Guid>(id => id.Value, v => new ProjectId(v));
    private sealed class WorkItemIdConverter() : ValueConverter<WorkItemId, Guid>(id => id.Value, v => new WorkItemId(v));
    private sealed class TimeEntryIdConverter() : ValueConverter<TimeEntryId, Guid>(id => id.Value, v => new TimeEntryId(v));
    private sealed class CommentIdConverter() : ValueConverter<CommentId, Guid>(id => id.Value, v => new CommentId(v));
    private sealed class AcceptanceCriterionIdConverter() : ValueConverter<AcceptanceCriterionId, Guid>(id => id.Value, v => new AcceptanceCriterionId(v));
    private sealed class AuditLogIdConverter() : ValueConverter<AuditLogId, Guid>(id => id.Value, v => new AuditLogId(v));
}
