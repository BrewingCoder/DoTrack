using DoTrack.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoTrack.Infrastructure.Persistence.Configurations;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> b)
    {
        b.ToTable("projects");
        b.HasKey(x => x.Id);
        b.Property(x => x.WorkspaceId).IsRequired();
        b.Property(x => x.Key).HasMaxLength(16).IsRequired();
        b.Property(x => x.Name).HasMaxLength(256).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2048);
        b.Property(x => x.NextWorkItemNumber).IsRequired().HasDefaultValue(1);
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();
        b.HasIndex(x => new { x.WorkspaceId, x.Key }).IsUnique();
        b.HasOne<Workspace>().WithMany().HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Restrict);
    }
}
