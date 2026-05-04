using DoTrack.Domain.Identity;
using DoTrack.Domain.WorkItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoTrack.Infrastructure.Persistence.Configurations;

internal sealed class AcceptanceCriterionConfiguration : IEntityTypeConfiguration<AcceptanceCriterion>
{
    public void Configure(EntityTypeBuilder<AcceptanceCriterion> b)
    {
        b.ToTable("acceptance_criteria");
        b.HasKey(x => x.Id);
        b.Property(x => x.WorkItemId).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2048).IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.CheckedByUserId);
        b.Property(x => x.CheckedAt);
        b.Property(x => x.Comment).HasMaxLength(2048);
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();
        b.HasIndex(x => x.WorkItemId);
        b.HasOne<WorkItem>().WithMany().HasForeignKey(x => x.WorkItemId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CheckedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
