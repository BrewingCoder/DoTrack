namespace DoTrack.Application.Abstractions;

public sealed record AuditContext(
    string Source,
    string? Reason = null,
    IReadOnlyDictionary<string, string>? Metadata = null);
