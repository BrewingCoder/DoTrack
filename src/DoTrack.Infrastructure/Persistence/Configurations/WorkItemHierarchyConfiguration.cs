using DoTrack.Domain.WorkItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoTrack.Infrastructure.Persistence.Configurations;

internal sealed class WorkItemHierarchyConfiguration : IEntityTypeConfiguration<WorkItemHierarchy>
{
    public void Configure(EntityTypeBuilder<WorkItemHierarchy> b)
    {
        b.ToTable("work_item_hierarchy");
        b.HasKey(x => new { x.AncestorId, x.DescendantId });
        b.Property(x => x.Depth).IsRequired();
        b.HasIndex(x => x.DescendantId);
        b.HasOne<WorkItem>().WithMany().HasForeignKey(x => x.AncestorId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<WorkItem>().WithMany().HasForeignKey(x => x.DescendantId).OnDelete(DeleteBehavior.Cascade);
    }
}
