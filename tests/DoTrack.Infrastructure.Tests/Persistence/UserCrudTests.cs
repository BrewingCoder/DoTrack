using DoTrack.Domain.Identity;
using DoTrack.Infrastructure.Tests.Builders;
using DoTrack.Infrastructure.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DoTrack.Infrastructure.Tests.Persistence;

public abstract class UserCrudTests<TFixture> : DatabaseTestBase<TFixture>
    where TFixture : class, IDbProviderFixture
{
    protected UserCrudTests(TFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Insert_RoundTripsAllFields()
    {
        var now = DateTimeOffset.UtcNow;
        var user = new User(UserId.New(), "alice@example.com", "Alice Anderson", now);

        await using (var write = CreateContext())
        {
            write.Users.Add(user);
            await write.SaveChangesAsync();
        }

        await using var read = CreateContext();
        var fetched = await read.Users.SingleAsync(u => u.Id == user.Id);
        fetched.Email.ShouldBe("alice@example.com");
        fetched.DisplayName.ShouldBe("Alice Anderson");
        fetched.Id.ShouldBe(user.Id);
    }

    [Fact]
    public async Task Email_AtMaxLength_320_Succeeds()
    {
        var local = new string('a', 64);
        var domain = string.Join(".", Enumerable.Range(0, 5).Select(_ => new string('b', 50)));
        var email = $"{local}@{domain[..253]}";
        email.Length.ShouldBe(318);
        var user = new User(UserId.New(), email, "Edge Length", DateTimeOffset.UtcNow);

        await using var ctx = CreateContext();
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var fetched = await ctx.Users.SingleAsync(u => u.Id == user.Id);
        fetched.Email.Length.ShouldBe(318);
    }

    [Fact]
    public async Task Email_OverMaxLength_Fails()
    {
        if (Fixture.ProviderName == "Sqlite")
        {
            Assert.Skip("SQLite does not enforce VARCHAR(N) length constraints.");
        }

        var oversized = new string('a', 321);
        var user = new User(UserId.New(), oversized, "Over Max", DateTimeOffset.UtcNow);

        await using var ctx = CreateContext();
        ctx.Users.Add(user);
        var act = () => ctx.SaveChangesAsync();
        await act.ShouldThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Email_DuplicateInsert_FailsUniqueConstraint()
    {
        var email = "dup@example.com";
        var u1 = new User(UserId.New(), email, "First", DateTimeOffset.UtcNow);
        var u2 = new User(UserId.New(), email, "Second", DateTimeOffset.UtcNow);

        await using (var ctx1 = CreateContext())
        {
            ctx1.Users.Add(u1);
            await ctx1.SaveChangesAsync();
        }

        await using var ctx2 = CreateContext();
        ctx2.Users.Add(u2);
        var act = () => ctx2.SaveChangesAsync();
        await act.ShouldThrowAsync<DbUpdateException>();
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("USER@EXAMPLE.COM")]
    [InlineData("user.name+tag@example.com")]
    [InlineData("user@subdomain.example.co.uk")]
    [InlineData("用户@example.com")]
    [InlineData("a@b.co")]
    public async Task Email_VariousFormats_RoundTrip(string email)
    {
        var user = new User(UserId.New(), email, "Format Test", DateTimeOffset.UtcNow);

        await using (var write = CreateContext())
        {
            write.Users.Add(user);
            await write.SaveChangesAsync();
        }

        await using var read = CreateContext();
        var fetched = await read.Users.SingleAsync(u => u.Id == user.Id);
        fetched.Email.ShouldBe(email);
    }

    [Fact]
    public async Task DisplayName_WhitespaceAndUnicode_Preserved()
    {
        var user = new User(UserId.New(), "ws@example.com", "  Pádded 名前  ", DateTimeOffset.UtcNow);

        await using (var write = CreateContext())
        {
            write.Users.Add(user);
            await write.SaveChangesAsync();
        }

        await using var read = CreateContext();
        var fetched = await read.Users.SingleAsync(u => u.Id == user.Id);
        fetched.DisplayName.ShouldBe("  Pádded 名前  ");
    }

    [Fact]
    public async Task UserId_StronglyTyped_RoundTripsThroughDb()
    {
        var id = UserId.New();
        var user = new User(id, "id@example.com", "Id Test", DateTimeOffset.UtcNow);

        await using (var write = CreateContext())
        {
            write.Users.Add(user);
            await write.SaveChangesAsync();
        }

        await using var read = CreateContext();
        var fetched = await read.Users.SingleOrDefaultAsync(u => u.Id == id);
        fetched.ShouldNotBeNull();
        fetched.Id.ShouldBe(id);
        fetched.Id.Value.ShouldBe(id.Value);
    }

    [Fact]
    public async Task Bogus_GeneratedUsers_Many_RoundTrip()
    {
        var users = UserBuilder.NewFaker(seed: 42).Generate(50);
        users = users.GroupBy(u => u.Email).Select(g => g.First()).ToList();

        await using (var write = CreateContext())
        {
            write.Users.AddRange(users);
            await write.SaveChangesAsync();
        }

        await using var read = CreateContext();
        (await read.Users.CountAsync()).ShouldBe(users.Count);
    }
}

[Collection(nameof(PostgresCollection))]
public sealed class UserCrudTests_Postgres(PostgresFixture fixture) : UserCrudTests<PostgresFixture>(fixture);

[Collection(nameof(SqlServerCollection))]
public sealed class UserCrudTests_SqlServer(SqlServerFixture fixture) : UserCrudTests<SqlServerFixture>(fixture);

[Collection(nameof(SqliteCollection))]
public sealed class UserCrudTests_Sqlite(SqliteFixture fixture) : UserCrudTests<SqliteFixture>(fixture);
