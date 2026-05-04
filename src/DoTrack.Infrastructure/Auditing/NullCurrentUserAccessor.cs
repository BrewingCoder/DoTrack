using DoTrack.Application.Abstractions;
using DoTrack.Domain.Identity;

namespace DoTrack.Infrastructure.Auditing;

public sealed class NullCurrentUserAccessor : ICurrentUserAccessor
{
    public UserId? CurrentUserId => null;
}
