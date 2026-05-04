using DoTrack.Application.AcceptanceCriteria;
using DoTrack.Application.Abstractions;
using DoTrack.Application.Auditing;
using DoTrack.Application.Comments;
using DoTrack.Application.Identity;
using DoTrack.Application.Milestones;
using DoTrack.Application.SavedQueries;
using DoTrack.Application.Sprints;
using DoTrack.Application.Time;
using DoTrack.Application.Webhooks;
using DoTrack.Application.WorkItems;
using DoTrack.Application.Workspaces;
using DoTrack.Infrastructure.AcceptanceCriteria;
using DoTrack.Infrastructure.Auditing;
using DoTrack.Infrastructure.Comments;
using DoTrack.Infrastructure.Identity;
using DoTrack.Infrastructure.Milestones;
using DoTrack.Infrastructure.Outbox;
using DoTrack.Infrastructure.Persistence;
using DoTrack.Infrastructure.SavedQueries;
using DoTrack.Infrastructure.Sprints;
using DoTrack.Infrastructure.Time;
using DoTrack.Infrastructure.Webhooks;
using DoTrack.Infrastructure.WorkItems;
using DoTrack.Infrastructure.Workspaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DoTrack.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDoTrackInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<OutboxEmitter>();
        services.AddScoped<IAuditContextAccessor, AmbientAuditContextAccessor>();
        services.TryAddDefaultCurrentUserAccessor();
        services.AddScoped<AuditingInterceptor>();
        services.AddScoped<IProjectResolver, ProjectResolver>();
        services.AddScoped<ICreateWorkItemHandler, CreateWorkItemHandler>();
        services.AddScoped<IGetWorkItemHandler, GetWorkItemHandler>();
        services.AddScoped<IListWorkItemsForProjectHandler, ListWorkItemsForProjectHandler>();
        services.AddScoped<IUpdateWorkItemHandler, UpdateWorkItemHandler>();
        services.AddScoped<ISetWorkItemParentHandler, SetWorkItemParentHandler>();
        services.AddScoped<IAddWorkItemLinkHandler, AddWorkItemLinkHandler>();
        services.AddScoped<IRemoveWorkItemLinkHandler, RemoveWorkItemLinkHandler>();
        services.AddScoped<IListWorkItemLinksHandler, ListWorkItemLinksHandler>();
        services.AddScoped<IAddCommentHandler, AddCommentHandler>();
        services.AddScoped<IListCommentsHandler, ListCommentsHandler>();
        services.AddScoped<ILogTimeHandler, LogTimeHandler>();
        services.AddScoped<IListTimeEntriesHandler, ListTimeEntriesHandler>();
        services.AddScoped<IGetEntityHistoryHandler, GetEntityHistoryHandler>();
        services.AddScoped<IAddCriterionHandler, AddCriterionHandler>();
        services.AddScoped<IUpdateCriterionStatusHandler, UpdateCriterionStatusHandler>();
        services.AddScoped<IListCriteriaHandler, ListCriteriaHandler>();
        services.AddScoped<ICreateSprintHandler, CreateSprintHandler>();
        services.AddScoped<IUpdateSprintHandler, UpdateSprintHandler>();
        services.AddScoped<IDeleteSprintHandler, DeleteSprintHandler>();
        services.AddScoped<IListSprintsHandler, ListSprintsHandler>();
        services.AddScoped<IAssignToSprintHandler, AssignToSprintHandler>();
        services.AddScoped<IListSprintWorkItemsHandler, ListSprintWorkItemsHandler>();
        services.AddScoped<ICreateWorkspaceHandler, CreateWorkspaceHandler>();
        services.AddScoped<IListWorkspacesHandler, ListWorkspacesHandler>();
        services.AddScoped<ICreateProjectHandler, CreateProjectHandler>();
        services.AddScoped<IListProjectsHandler, ListProjectsHandler>();
        services.AddScoped<ICreateUserHandler, CreateUserHandler>();
        services.AddScoped<IListUsersHandler, ListUsersHandler>();
        services.AddScoped<ICreateMilestoneHandler, CreateMilestoneHandler>();
        services.AddScoped<IUpdateMilestoneHandler, UpdateMilestoneHandler>();
        services.AddScoped<IDeleteMilestoneHandler, DeleteMilestoneHandler>();
        services.AddScoped<IListMilestonesHandler, ListMilestonesHandler>();
        services.AddScoped<IAddScopeItemHandler, AddScopeItemHandler>();
        services.AddScoped<IRemoveScopeItemHandler, RemoveScopeItemHandler>();
        services.AddScoped<IGetMilestoneScopeHandler, GetMilestoneScopeHandler>();
        services.AddScoped<IGetMilestoneHealthHandler, GetMilestoneHealthHandler>();
        services.AddScoped<ICreateSavedQueryHandler, CreateSavedQueryHandler>();
        services.AddScoped<IUpdateSavedQueryHandler, UpdateSavedQueryHandler>();
        services.AddScoped<IDeleteSavedQueryHandler, DeleteSavedQueryHandler>();
        services.AddScoped<IListSavedQueriesHandler, ListSavedQueriesHandler>();
        services.AddScoped<IFindByIssueKeyHandler, FindByIssueKeyHandler>();
        services.AddScoped<IWebhookEventDispatcher, WebhookEventDispatcher>();
        services.AddScoped<IWatchWorkItemHandler, WatchWorkItemHandler>();
        services.AddScoped<IUnwatchWorkItemHandler, UnwatchWorkItemHandler>();
        services.AddScoped<IListWatchersHandler, ListWatchersHandler>();
        services.AddScoped<IMyWorkHandler, MyWorkHandler>();

        services.AddDbContext<DoTrackDbContext>((sp, options) =>
        {
            configureDb(options);
            options.AddInterceptors(sp.GetRequiredService<AuditingInterceptor>());
        });

        return services;
    }

    private static void TryAddDefaultCurrentUserAccessor(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserAccessor, NullCurrentUserAccessor>();
    }
}
