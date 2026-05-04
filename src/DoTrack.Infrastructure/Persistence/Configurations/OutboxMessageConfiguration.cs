using DoTrack.Domain.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoTrack.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("outbox_messages");
        b.HasKey(x => x.Id);
        b.Property(x => x.EventType).HasMaxLength(128).IsRequired();
        b.Property(x => x.PayloadJson).IsRequired();
        b.Property(x => x.ProjectKey).HasMaxLength(64).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.DeliveredAt);
        b.Property(x => x.Attempts).IsRequired();
        b.Property(x => x.LastAttemptAt);
        b.Property(x => x.LastError).HasMaxLength(2048);
        b.HasIndex(x => x.DeliveredAt);
        b.HasIndex(x => x.CreatedAt);
    }
}
