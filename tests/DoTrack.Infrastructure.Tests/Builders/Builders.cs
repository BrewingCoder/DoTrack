using Bogus;
using DoTrack.Domain.Identity;
using DoTrack.Domain.WorkItems;
using DoTrack.Domain.Workspaces;

namespace DoTrack.Infrastructure.Tests.Builders;

public static class UserBuilder
{
    public static Faker<User> NewFaker(int? seed = null)
    {
        var faker = new Faker<User>()
            .CustomInstantiator(f => new User(
                UserId.New(),
                f.Internet.Email(),
                f.Name.FullName(),
                DateTimeOffset.UtcNow));
        if (seed is not null)
        {
            faker.UseSeed(seed.Value);
        }
        return faker;
    }

    public static User One(int? seed = null) => NewFaker(seed).Generate();
}

public static class WorkspaceBuilder
{
    public static Faker<Workspace> NewFaker(int? seed = null)
    {
        var faker = new Faker<Workspace>()
            .CustomInstantiator(f => new Workspace(
                WorkspaceId.New(),
                f.Company.CompanyName(),
                f.Lorem.Slug(),
                DateTimeOffset.UtcNow));
        if (seed is not null)
        {
            faker.UseSeed(seed.Value);
        }
        return faker;
    }

    public static Workspace One(int? seed = null) => NewFaker(seed).Generate();
}

public static class ProjectBuilder
{
    public static Project One(WorkspaceId workspaceId, string? key = null, int? seed = null)
    {
        var faker = new Faker();
        if (seed is not null)
        {
            faker = new Faker();
        }
        return new Project(
            ProjectId.New(),
            workspaceId,
            key ?? faker.Random.AlphaNumeric(4).ToUpperInvariant(),
            faker.Commerce.ProductName(),
            faker.Lorem.Sentence(),
            DateTimeOffset.UtcNow);
    }
}

public static class WorkItemBuilder
{
    public static WorkItem One(
        ProjectId projectId,
        UserId reporterId,
        WorkItemTier tier = WorkItemTier.Item,
        WorkItemType? type = WorkItemType.Task,
        int number = 1,
        string? title = null,
        string? description = null,
        UserId? assigneeId = null,
        int? estimatePoints = null)
    {
        var faker = new Faker();
        return new WorkItem(
            WorkItemId.New(),
            projectId,
            number,
            tier,
            tier == WorkItemTier.Item ? (type ?? WorkItemType.Task) : null,
            title ?? faker.Hacker.Phrase(),
            description,
            reporterId,
            assigneeId,
            estimatePoints,
            DateTimeOffset.UtcNow);
    }
}
