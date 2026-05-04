using DoTrack.Domain.Identity;

namespace DoTrack.Application.Abstractions;

public interface ICurrentUserAccessor
{
    UserId? CurrentUserId { get; }
}
