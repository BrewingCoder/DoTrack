namespace DoTrack.Application.Abstractions;

public interface IAuditContextAccessor
{
    AuditContext? Current { get; }
    void SetContext(AuditContext context);
}
