using System.Reflection;
using System.Text.Json;
using DoTrack.Application.Abstractions;
using DoTrack.Domain.Auditing;
using DoTrack.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DoTrack.Infrastructure.Auditing;

public sealed class AuditingInterceptor(
    ICurrentUserAccessor currentUserAccessor,
    IAuditContextAccessor auditContextAccessor,
    TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AppendAuditEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AppendAuditEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AppendAuditEntries(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var userId = currentUserAccessor.CurrentUserId;
        var auditContext = auditContextAccessor.Current ?? new AuditContext("system");
        var metadataJson = auditContext.Metadata is { Count: > 0 } meta
            ? JsonSerializer.Serialize(meta)
            : null;

        var auditLogs = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var entityType = entry.Entity.GetType();
            if (entityType.GetCustomAttribute<NotAuditedAttribute>(inherit: false) is not null)
            {
                continue;
            }

            var changeType = entry.State switch
            {
                EntityState.Added => ChangeType.Insert,
                EntityState.Modified => ChangeType.Update,
                EntityState.Deleted => ChangeType.Delete,
                _ => throw new InvalidOperationException("Unreachable")
            };

            var fieldChanges = BuildFieldChanges(entry).ToList();
            if (changeType == ChangeType.Update && fieldChanges.Count == 0)
            {
                continue;
            }

            var entityId = ResolvePrimaryKeyAsString(entry);

            auditLogs.Add(new AuditLog(
                AuditLogId.New(),
                entityType.Name,
                entityId,
                changeType,
                userId,
                now,
                auditContext.Source,
                auditContext.Reason,
                metadataJson,
                fieldChanges));
        }

        if (auditLogs.Count > 0)
        {
            context.Set<AuditLog>().AddRange(auditLogs);
        }
    }

    private static IEnumerable<FieldChange> BuildFieldChanges(EntityEntry entry)
    {
        foreach (var prop in entry.Properties)
        {
            var propertyInfo = prop.Metadata.PropertyInfo;
            if (propertyInfo?.GetCustomAttribute<NotAuditedAttribute>(inherit: false) is not null)
            {
                continue;
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    if (prop.CurrentValue is not null)
                    {
                        yield return new FieldChange(prop.Metadata.Name, null, FormatValue(prop.CurrentValue));
                    }
                    break;
                case EntityState.Modified:
                    if (prop.IsModified)
                    {
                        yield return new FieldChange(
                            prop.Metadata.Name,
                            FormatValue(prop.OriginalValue),
                            FormatValue(prop.CurrentValue));
                    }
                    break;
                case EntityState.Deleted:
                    yield return new FieldChange(prop.Metadata.Name, FormatValue(prop.OriginalValue), null);
                    break;
            }
        }
    }

    private static string? FormatValue(object? value) => value switch
    {
        null => null,
        UserId u => u.Value.ToString(),
        DateTimeOffset dto => dto.ToString("O"),
        DateTime dt => dt.ToString("O"),
        Enum e => e.ToString(),
        _ => value.ToString()
    };

    private static string ResolvePrimaryKeyAsString(EntityEntry entry)
    {
        var pk = entry.Metadata.FindPrimaryKey();
        if (pk is null)
        {
            return "<no-pk>";
        }
        var values = pk.Properties.Select(p => FormatValue(entry.Property(p.Name).CurrentValue) ?? "<null>");
        return string.Join(",", values);
    }
}
