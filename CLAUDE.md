# CLAUDE.md — agent instructions for DoTrack

Self-hosted OSS issue tracker / project manager. .NET 10 backend with EF Core multi-provider (Postgres / SQL Server / SQLite). React frontend deferred until a UX-reference rig is populated.

## Read first

- [docs/TESTING.md](docs/TESTING.md) — testing discipline, multi-provider matrix, fixture patterns, library set.
- [docs/CONVENTIONS.md](docs/CONVENTIONS.md) — endpoint, handler, validator, commit, code style.
- [docs/DECISIONS.md](docs/DECISIONS.md) — locked architectural decisions.
- [docs/AUTONOMOUS_MODE.md](docs/AUTONOMOUS_MODE.md) — runbook for extended autonomous sessions.
- [DESIGN.md](DESIGN.md) — original product design.
- [SESSION_LOG.md](SESSION_LOG.md) — phase-by-phase index of prior work.

## Hard rules

1. Tests must push boundaries, not just verify happy path. Boundary values, null/empty, unicode, error paths, concurrency where relevant. Don't ship a test class that only proves the happy path. Detail in `docs/TESTING.md`.
2. Every persistence-layer test runs against all three providers via `DatabaseTestBase<TFixture>` + three concrete derivations.
3. HTTP integration tests share one `DoTrackApiFactory` per assembly via `[Collection(nameof(IntegrationCollection))]`. Don't use `IClassFixture<DoTrackApiFactory>`.
4. `User` entity is `[NotAudited]`. Property-level `[NotAudited]` is the supported way to filter sequence-only or other internal-counter fields out of audit diffs.
5. Closure tables and other shapes with multiple FK paths to the same table use `OnDelete(NoAction)`. SQL Server rejects multi-cascade-paths; app code handles cleanup.
6. SQLite limitations apply uniformly: never `ORDER BY DateTimeOffset` server-side (sort client-side after `ToListAsync`); `VARCHAR(N)` length is not enforced (skip those tests with `Fixture.ProviderName == "Sqlite"`).
7. PATCH endpoints use null-means-no-change semantics. Explicit clear/unassign requires a separate endpoint.
8. Outbox emission belongs in the same `SaveChanges` as the domain change. Use `OutboxEmitter.Emit(...)` before `SaveChangesAsync`.
9. Don't commit unless explicitly asked. Don't push without explicit ask.
10. Don't start UI work without the YouTrack reference rig running at `localhost:8888`. Compose file at `.dev/youtrack-ref/`.

## Stack anchors

- .NET 10 + EF Core multi-provider
- xUnit.v3 + Shouldly + NSubstitute + Bogus + Verify.XunitV3
- Apache 2.0
- React + shadcn/ui + Tailwind + TipTap + Vaul (PWA) — frontend deferred
- No JVM languages anywhere in the stack

## Pattern templates

When adding a new use case, copy the existing pattern. Reference implementations:

- Use case end-to-end: `CreateWorkItem` (Application command + Infrastructure handler + Api endpoint + multi-provider tests)
- Endpoint group: `src/DoTrack.Api/WorkItems/WorkItemEndpoints.cs`
- Adapter: `src/DoTrack.GitProviders.GitHub/GitHubAdapter.cs`

`docs/CONVENTIONS.md` walks the templates.

## Repository state

`SESSION_LOG.md` lists every phase committed so far. `README.md` documents the API surface that's online.
