using DoTrack.Application.SavedQueries;
using DoTrack.Domain.SavedQueries;
using DoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DoTrack.Infrastructure.SavedQueries;

public sealed class CreateSavedQueryHandler(DoTrackDbContext db, TimeProvider timeProvider) : ICreateSavedQueryHandler
{
    public async Task<CreateSavedQueryResult> HandleAsync(CreateSavedQueryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var ownerExists = await db.Users.AnyAsync(u => u.Id == command.OwnerUserId, cancellationToken);
        if (!ownerExists)
        {
            throw new InvalidOperationException($"Owner '{command.OwnerUserId.Value}' not found.");
        }
        if (command.ProjectId is { } pid)
        {
            var projectExists = await db.Projects.AnyAsync(p => p.Id == pid, cancellationToken);
            if (!projectExists)
            {
                throw new InvalidOperationException($"Project '{pid.Value}' not found.");
            }
        }

        var saved = new SavedQuery(
            SavedQueryId.New(), command.OwnerUserId, command.Scope, command.ProjectId,
            command.Name, command.QueryText, command.Color, command.Icon, timeProvider.GetUtcNow());
        db.SavedQueries.Add(saved);
        await db.SaveChangesAsync(cancellationToken);
        return new CreateSavedQueryResult(saved.Id);
    }
}

public sealed class UpdateSavedQueryHandler(DoTrackDbContext db, TimeProvider timeProvider) : IUpdateSavedQueryHandler
{
    public async Task HandleAsync(UpdateSavedQueryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var saved = await db.SavedQueries.SingleOrDefaultAsync(s => s.Id == command.Id, cancellationToken)
            ?? throw new InvalidOperationException($"SavedQuery '{command.Id.Value}' not found.");
        saved.Update(command.Name, command.QueryText, command.Color, command.Icon, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class DeleteSavedQueryHandler(DoTrackDbContext db) : IDeleteSavedQueryHandler
{
    public async Task HandleAsync(DeleteSavedQueryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await db.SavedQueries.Where(s => s.Id == command.Id).ExecuteDeleteAsync(cancellationToken);
    }
}

public sealed class ListSavedQueriesHandler(DoTrackDbContext db) : IListSavedQueriesHandler
{
    public async Task<IReadOnlyList<SavedQuery>> HandleAsync(ListSavedQueriesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = db.SavedQueries.AsQueryable();
        var visible = q.Where(s =>
            (s.Scope == SavedQueryScope.Personal && query.RequestingUserId != null && s.OwnerUserId == query.RequestingUserId.Value)
            || (s.Scope == SavedQueryScope.Project && query.ProjectId != null && s.ProjectId == query.ProjectId.Value)
            || (s.Scope == SavedQueryScope.Public && query.IncludePublic));

        var rows = await visible.ToListAsync(cancellationToken);
        return rows.OrderBy(s => s.Name).ToList();
    }
}
