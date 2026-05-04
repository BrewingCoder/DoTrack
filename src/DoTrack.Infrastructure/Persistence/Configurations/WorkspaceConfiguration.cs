using DoTrack.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoTrack.Infrastructure.Persistence.Configurations;

internal sealed class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> b)
    {
        b.ToTable("workspaces");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(256).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(64).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
    }
}
