using DoTrack.Domain.Identity;
using DoTrack.Domain.SavedQueries;
using DoTrack.Domain.Workspaces;

namespace DoTrack.Application.SavedQueries;

public sealed record CreateSavedQueryCommand(
    UserId OwnerUserId,
    SavedQueryScope Scope,
    ProjectId? ProjectId,
    string Name,
    string QueryText,
    string? Color,
    string? Icon);
public sealed record CreateSavedQueryResult(SavedQueryId Id);
public interface ICreateSavedQueryHandler
{
    Task<CreateSavedQueryResult> HandleAsync(CreateSavedQueryCommand command, CancellationToken cancellationToken);
}

public sealed record UpdateSavedQueryCommand(
    SavedQueryId Id, string? Name, string? QueryText, string? Color, string? Icon);
public interface IUpdateSavedQueryHandler
{
    Task HandleAsync(UpdateSavedQueryCommand command, CancellationToken cancellationToken);
}

public sealed record DeleteSavedQueryCommand(SavedQueryId Id);
public interface IDeleteSavedQueryHandler
{
    Task HandleAsync(DeleteSavedQueryCommand command, CancellationToken cancellationToken);
}

public sealed record ListSavedQueriesQuery(UserId? RequestingUserId, ProjectId? ProjectId, bool IncludePublic);
public interface IListSavedQueriesHandler
{
    Task<IReadOnlyList<SavedQuery>> HandleAsync(ListSavedQueriesQuery query, CancellationToken cancellationToken);
}
