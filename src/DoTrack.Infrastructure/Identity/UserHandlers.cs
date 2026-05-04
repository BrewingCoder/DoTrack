using DoTrack.Application.Identity;
using DoTrack.Domain.Identity;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.Identity;

public sealed class CreateUserHandler(DoTrackDbContext db, TimeProvider timeProvider) : ICreateUserHandler
{
    public async Task<CreateUserResult> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var emailTaken = await db.Users.AnyAsync(u => u.Email == command.Email, cancellationToken);
        if (emailTaken)
        {
            throw new InvalidOperationException($"Email '{command.Email}' is already in use.");
        }

        var user = new User(UserId.New(), command.Email, command.DisplayName, timeProvider.GetUtcNow());
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return new CreateUserResult(user.Id);
    }
}

public sealed class ListUsersHandler(DoTrackDbContext db) : IListUsersHandler
{
    public async Task<IReadOnlyList<User>> HandleAsync(CancellationToken cancellationToken)
    {
        var rows = await db.Users.ToListAsync(cancellationToken);
        return rows.OrderBy(u => u.Email).ToList();
    }
}
