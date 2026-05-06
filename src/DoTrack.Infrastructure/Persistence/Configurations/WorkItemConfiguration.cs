using DoTrack.Domain.Identity;
using DoTrack.Domain.Sprints;
using DoTrack.Domain.WorkItems;
using DoTrack.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoTrack.Infrastructure.Persistence.Configurations;

internal sealed class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
    public void Configure(EntityTypeBuilder<WorkItem> b)
    {
        b.ToTable("work_items");
        b.HasKey(x => x.Id);
        b.Property(x => x.ProjectId).IsRequired();
        b.Property(x => x.Number).IsRequired();
        b.Property(x => x.Tier).HasConversion<int>().IsRequired();
        b.Property(x => x.Type).HasConversion<int?>();
        b.Property(x => x.State).HasConversion<int>().IsRequired();
        b.Property(x => x.Priority).HasConversion<int>().IsRequired();
        b.Property(x => x.Title).HasMaxLength(512).IsRequired();
        b.Property(x => x.Description);
        b.Property(x => x.ReporterId).IsRequired();
        b.Property(x => x.AssigneeId);
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();
        b.Property(x => x.SprintId);
        b.HasIndex(x => new { x.ProjectId, x.Number }).IsUnique();
        b.HasIndex(x => x.State);
        b.HasIndex(x => x.AssigneeId);
        b.HasIndex(x => x.SprintId);
        b.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ReporterId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.AssigneeId).OnDelete(DeleteBehavior.SetNull);
        // NoAction to avoid SQL Server's "multiple cascade paths" rejection
        // (Project -> Sprint -> WorkItem.SprintId vs Project -> WorkItem direct).
        // App code must null out WorkItem.SprintId before deleting the Sprint.
        b.HasOne<Sprint>().WithMany().HasForeignKey(x => x.SprintId).OnDelete(DeleteBehavior.NoAction);
    }
}
