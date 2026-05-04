using DoTrack.Domain.Comments;
using DoTrack.Domain.Identity;
using DoTrack.Domain.WorkItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoTrack.Infrastructure.Persistence.Configurations;

internal sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> b)
    {
        b.ToTable("comments");
        b.HasKey(x => x.Id);
        b.Property(x => x.WorkItemId).IsRequired();
        b.Property(x => x.AuthorId).IsRequired();
        b.Property(x => x.Body).IsRequired();
        b.Property(x => x.IsInternal).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
        b.HasIndex(x => x.WorkItemId);
        b.HasOne<WorkItem>().WithMany().HasForeignKey(x => x.WorkItemId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
    }
}
