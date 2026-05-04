using DoTrack.Domain.Identity;
using DoTrack.Domain.WorkItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoTrack.Infrastructure.Persistence.Configurations;

internal sealed class WorkItemWatcherConfiguration : IEntityTypeConfiguration<WorkItemWatcher>
{
    public void Configure(EntityTypeBuilder<WorkItemWatcher> b)
    {
        b.ToTable("work_item_watchers");
        b.HasKey(x => new { x.WorkItemId, x.UserId });
        b.Property(x => x.AddedAt).IsRequired();
        b.HasIndex(x => x.UserId);
        b.HasOne<WorkItem>().WithMany().HasForeignKey(x => x.WorkItemId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
