using System.Text.Json;
using DoTrack.Domain.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoTrack.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.HasKey(x => x.Id);

        b.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
        b.Property(x => x.EntityId).HasMaxLength(128).IsRequired();
        b.Property(x => x.ChangeType).HasConversion<int>().IsRequired();
        b.Property(x => x.ChangedByUserId);
        b.Property(x => x.OccurredAt).IsRequired();
        b.Property(x => x.Source).HasMaxLength(32).IsRequired();
        b.Property(x => x.ChangeReason).HasMaxLength(2048);
        b.Property(x => x.SourceMetadataJson);

        b.Property(x => x.FieldChanges)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<FieldChange>>(v, (JsonSerializerOptions?)null) ?? new List<FieldChange>())
            .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<FieldChange>>(
                (a, c) => (a == null && c == null) || (a != null && c != null && a.SequenceEqual(c)),
                v => v.Aggregate(0, (acc, f) => HashCode.Combine(acc, f)),
                v => v.ToList()));

        b.HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredAt });
        b.HasIndex(x => new { x.ChangedByUserId, x.OccurredAt });
        b.HasIndex(x => x.OccurredAt);
    }
}
