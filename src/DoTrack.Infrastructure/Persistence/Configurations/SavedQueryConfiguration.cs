using DoTrack.Domain.Identity;
using DoTrack.Domain.SavedQueries;
using DoTrack.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoTrack.Infrastructure.Persistence.Configurations;

internal sealed class SavedQueryConfiguration : IEntityTypeConfiguration<SavedQuery>
{
    public void Configure(EntityTypeBuilder<SavedQuery> b)
    {
        b.ToTable("saved_queries");
        b.HasKey(x => x.Id);
        b.Property(x => x.OwnerUserId).IsRequired();
        b.Property(x => x.Scope).HasConversion<int>().IsRequired();
        b.Property(x => x.ProjectId);
        b.Property(x => x.Name).HasMaxLength(256).IsRequired();
        b.Property(x => x.QueryText).IsRequired();
        b.Property(x => x.Color).HasMaxLength(32);
        b.Property(x => x.Icon).HasMaxLength(64);
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();
        b.HasIndex(x => x.OwnerUserId);
        b.HasIndex(x => x.ProjectId);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.NoAction);
    }
}
