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
        // Both FKs are NoAction because SQL Server rejects multiple cascade paths to the same table.
        // Closure-table cleanup is the app's responsibility: when deleting a WorkItem, app code must
        // explicitly remove rows where it appears as ancestor OR descendant before the WorkItem delete.
        b.HasOne<WorkItem>().WithMany().HasForeignKey(x => x.AncestorId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<WorkItem>().WithMany().HasForeignKey(x => x.DescendantId).OnDelete(DeleteBehavior.NoAction);
    }
}
