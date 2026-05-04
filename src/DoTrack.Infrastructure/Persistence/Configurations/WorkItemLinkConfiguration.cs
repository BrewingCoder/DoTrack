using DoTrack.Domain.Identity;
using DoTrack.Domain.WorkItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoTrack.Infrastructure.Persistence.Configurations;

internal sealed class WorkItemLinkConfiguration : IEntityTypeConfiguration<WorkItemLink>
{
    public void Configure(EntityTypeBuilder<WorkItemLink> b)
    {
        b.ToTable("work_item_links");
        b.HasKey(x => x.Id);
        b.Property(x => x.SourceId).IsRequired();
        b.Property(x => x.TargetId).IsRequired();
        b.Property(x => x.LinkType).HasConversion<int>().IsRequired();
        b.Property(x => x.CreatedByUserId);
        b.Property(x => x.CreatedAt).IsRequired();
        b.HasIndex(x => x.SourceId);
        b.HasIndex(x => x.TargetId);
        b.HasIndex(x => new { x.SourceId, x.TargetId, x.LinkType }).IsUnique();
        // NoAction so SQL Server doesn't complain about cascade paths
        // (deleting a WorkItem -> cascade -> two FK columns on this table).
        // App owns cleanup: when deleting a WorkItem, app must clear its links.
        b.HasOne<WorkItem>().WithMany().HasForeignKey(x => x.SourceId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<WorkItem>().WithMany().HasForeignKey(x => x.TargetId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
