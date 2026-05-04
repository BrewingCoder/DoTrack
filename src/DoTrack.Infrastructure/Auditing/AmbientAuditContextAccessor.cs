using DoTrack.Application.Abstractions;

namespace DoTrack.Infrastructure.Auditing;

public sealed class AmbientAuditContextAccessor : IAuditContextAccessor
{
    public AuditContext? Current { get; private set; }
    public void SetContext(AuditContext context) => Current = context;
}
