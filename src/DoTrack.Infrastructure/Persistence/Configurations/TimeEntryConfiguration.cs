using DoTrack.Domain.Identity;
using DoTrack.Domain.Time;
using DoTrack.Domain.WorkItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoTrack.Infrastructure.Persistence.Configurations;

internal sealed class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> b)
    {
        b.ToTable("time_entries");
        b.HasKey(x => x.Id);
        b.Property(x => x.WorkItemId).IsRequired();
        b.Property(x => x.UserId).IsRequired();
        b.Property(x => x.StartedAt).IsRequired();
        b.Property(x => x.Duration).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2048).IsRequired();
        b.Property(x => x.Billable).IsRequired();
        b.Property(x => x.ActivityType).HasMaxLength(64);
        b.Property(x => x.CreatedAt).IsRequired();
        b.HasIndex(x => x.WorkItemId);
        b.HasIndex(x => new { x.UserId, x.StartedAt });
        b.HasOne<WorkItem>().WithMany().HasForeignKey(x => x.WorkItemId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
