using DoTrack.Domain.Milestones;
using DoTrack.Domain.WorkItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoTrack.Infrastructure.Persistence.Configurations;

internal sealed class MilestoneConfiguration : IEntityTypeConfiguration<Milestone>
{
    public void Configure(EntityTypeBuilder<Milestone> b)
    {
        b.ToTable("milestones");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(256).IsRequired();
        b.Property(x => x.Description).HasMaxLength(4096);
        b.Property(x => x.TargetDate);
        b.Property(x => x.HoursBudget).HasPrecision(10, 2);
        b.Property(x => x.VisibleToClient).IsRequired();
        b.Property(x => x.State).HasConversion<int>().IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();
        b.HasIndex(x => x.State);
    }
}

internal sealed class MilestoneScopeConfiguration : IEntityTypeConfiguration<MilestoneScope>
{
    public void Configure(EntityTypeBuilder<MilestoneScope> b)
    {
        b.ToTable("milestone_scope");
        b.HasKey(x => new { x.MilestoneId, x.WorkItemId });
        b.Property(x => x.AddedAt).IsRequired();
        b.HasIndex(x => x.WorkItemId);
        // NoAction on MilestoneId to avoid SQL Server cascade-path issue (deleting
        // a Milestone is the app's responsibility — clear scope rows first).
        b.HasOne<Milestone>().WithMany().HasForeignKey(x => x.MilestoneId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<WorkItem>().WithMany().HasForeignKey(x => x.WorkItemId).OnDelete(DeleteBehavior.Cascade);
    }
}
