# Testing

## Discipline

Tests must push boundaries, not just verify the happy path. Every new test class deliberately exercises edge cases. Don't ship a test class with only `Should_Work_When_Valid`.

When designing a test class, write the happy-path test, then immediately ask "what's the next five things that could go wrong?" and write tests for each.

## Library set

- **xUnit.v3** — runner
- **Shouldly** — assertions
- **NSubstitute** — mocks/fakes
- **Bogus** — fake data generation
- **Verify.XunitV3** — snapshot testing

Don't add Moq, AwesomeAssertions, or FluentAssertions.

## Test naming

`Method_Scenario_ExpectedBehavior`. CA1707 (no underscores) is suppressed for test projects so this convention reads cleanly.

## Multi-provider matrix

Every persistence-layer test class extends `DatabaseTestBase<TFixture>` and is derived three times:

```csharp
[Collection(nameof(PostgresCollection))]
public sealed class XCrudTests_Postgres(PostgresFixture f) : XCrudTests<PostgresFixture>(f);

[Collection(nameof(SqlServerCollection))]
public sealed class XCrudTests_SqlServer(SqlServerFixture f) : XCrudTests<SqlServerFixture>(f);

[Collection(nameof(SqliteCollection))]
public sealed class XCrudTests_Sqlite(SqliteFixture f) : XCrudTests<SqliteFixture>(f);
```

`DatabaseTestBase<TFixture>` wipes all tables before each test (FK-safe order) via `ExecuteDeleteAsync`. When adding a new entity that participates in tests, update both `DatabaseTestBase`'s cleanup and `DoTrackApiFactory.ResetDataAsync`.

Postgres + SQL Server fixtures use Testcontainers. SQLite uses an in-memory connection that stays open for the fixture's lifetime.

## HTTP tests

Single-provider (Postgres) via `DoTrackApiFactory : WebApplicationFactory<Program>`. All HTTP test classes share the factory via `[Collection(nameof(IntegrationCollection))]`. Don't use `IClassFixture<DoTrackApiFactory>` — multiple `WebApplicationFactory<Program>` instances collide on the implicit Program type.

Use `ApiJsonOptions.Default` for both serialization (`PostAsJsonAsync`) and deserialization (`ReadFromJsonAsync`). Camel-case + `JsonStringEnumConverter`.

### Test isolation

The factory's `InitializeAsync` carries a regression guard that asserts the resolved DbContext is bound to a connection string containing `dotrack_integration` (the testcontainer's database name). Without it, eager configuration reads in `AddConfiguredDatabase` silently routed every integration test to the developer's `dotrack_dev` Postgres on `:5433`. The fix lives in `src/DoTrack.Api/Configuration/DatabaseRegistration.cs` — config must be resolved inside the `AddDbContext` factory delegate, not at registration time. If the guard ever fires, that's the regression to look for.

## Provider quirks

### SQLite — VARCHAR length

`VARCHAR(N)` length is not enforced by SQLite. Tests that depend on max-length rejection skip on SQLite:

```csharp
if (Fixture.ProviderName == "Sqlite")
{
    Assert.Skip("SQLite does not enforce VARCHAR(N) length constraints.");
}
```

### SQLite — `ORDER BY DateTimeOffset`

`ORDER BY` on a `DateTimeOffset` column is not supported server-side. Materialise via `ToListAsync`, then sort client-side. Applied uniformly across providers — uniform behaviour beats per-provider optimisation at this scale.

A monotonic `Sequence` column on high-volume tables (`audit_logs`, `comments`, `time_entries`) is the path forward when the client-side sort starts to matter.

### SQL Server — multi-cascade-paths

SQL Server rejects multiple cascade paths to the same table. Closure tables and similar join shapes use `OnDelete(NoAction)`; the app removes the join rows before deleting the related entity.

## Boundary checklist for a new test class

- Happy path
- Each validation rule rejected at its own boundary (length max, max-1, max+1)
- null / empty / whitespace / unicode for every string property
- Negative / zero / max for numerics
- Each branch of error paths
- Cascade / restrict FK behaviour where relevant
- Audit row produced with the right shape
- `[NotAudited]`-only changes do not produce an audit row
- Multi-provider parity (no provider-specific test passes a check the others fail silently)

## Suppressed analyzer rules in test projects

`Directory.Build.props` suppresses, in test projects only:

- `xUnit1051` — cancellation-token nudge on every async assertion
- `CA1707` — fights `Method_Scenario_ExpectedBehavior` naming
- `CA1711` — xUnit collection types intentionally end in `Collection`
- `CA1816` — xUnit fixture `DisposeAsync` pattern

Production code retains the strict settings.
