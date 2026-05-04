using DoTrack.Domain.Sprints;
using DoTrack.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoTrack.Infrastructure.Persistence.Configurations;

internal sealed class SprintConfiguration : IEntityTypeConfiguration<Sprint>
{
    public void Configure(EntityTypeBuilder<Sprint> b)
    {
        b.ToTable("sprints");
        b.HasKey(x => x.Id);
        b.Property(x => x.ProjectId).IsRequired();
        b.Property(x => x.Name).HasMaxLength(256).IsRequired();
        b.Property(x => x.StartsOn).IsRequired();
        b.Property(x => x.EndsOn).IsRequired();
        b.Property(x => x.State).HasConversion<int>().IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();
        b.HasIndex(x => x.ProjectId);
        b.HasIndex(x => new { x.ProjectId, x.State });
        b.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
    }
}
