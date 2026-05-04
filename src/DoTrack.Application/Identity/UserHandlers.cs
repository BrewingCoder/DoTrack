using DoTrack.Domain.Identity;

namespace DoTrack.Application.Identity;

public sealed record CreateUserCommand(string Email, string DisplayName);
public sealed record CreateUserResult(UserId Id);
public interface ICreateUserHandler
{
    Task<CreateUserResult> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken);
}

public interface IListUsersHandler
{
    Task<IReadOnlyList<User>> HandleAsync(CancellationToken cancellationToken);
}
