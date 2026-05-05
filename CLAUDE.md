# CLAUDE.md — agent instructions for DoTrack

Self-hosted OSS issue tracker / project manager. .NET 10 backend with EF Core multi-provider (Postgres / SQL Server / SQLite). React + Vite + Tailwind + shadcn/ui frontend at `frontend/`, read-only in v0 (workspaces / projects / work items / work item detail).

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
10. Don't start a new UI feature without the YouTrack reference rig running at `localhost:8888`. Compose file at `.dev/youtrack-ref/` (or via the `dotrack` umbrella at `.dev/docker-compose.yml`). The SPA emulates YouTrack's information architecture and visual tokens — mirror the YT counterpart first, capture tokens from the running rig, then map onto the shadcn variable names already wired in `frontend/src/index.css`. Detail in `docs/DECISIONS.md` and `docs/CONVENTIONS.md`.
11. `AddConfiguredDatabase` reads `IConfiguration` *inside* the `AddDbContext` factory delegate, not at registration time. Reverting to eager registration silently routes integration tests to the dev rig DB. The regression guard in `DoTrackApiFactory.InitializeAsync` will fail any test run that does this.
12. Add `.Produces<T>(StatusCodes.Status200OK)` to GET endpoints that the UI consumes. Without it, NSwag emits `void`/`any` returns. Path A (annotate-as-you-go) is the locked strategy — see `docs/DECISIONS.md`.
13. After adding/modifying endpoints the UI uses, regenerate the TS client with `pnpm --dir frontend gen:api` against the running API. Commit the regenerated file with the change.

## Stack anchors

- .NET 10 + EF Core multi-provider
- xUnit.v3 + Shouldly + NSubstitute + Bogus + Verify.XunitV3
- Apache 2.0
- Vite + React 19 + TypeScript + Tailwind v4 + shadcn/ui (Nova preset) at `frontend/`. System-ui font stack to match the YouTrack reference (Geist dropped 2026-05-05).
- TanStack Query 5 + TanStack Router 1 (code-based routing)
- NSwag for typed TS client gen against the running API at `:5259`
- No JVM languages anywhere in the stack

## Pattern templates

When adding a new use case, copy the existing pattern. Reference implementations:

- Use case end-to-end: `CreateWorkItem` (Application command + Infrastructure handler + Api endpoint + multi-provider tests)
- Read-only use case: `ListWorkItemsForProject` (query + handler + endpoint + multi-provider integration tests + HTTP integration tests, all in one phase)
- Endpoint group: `src/DoTrack.Api/WorkItems/WorkItemEndpoints.cs`
- Adapter: `src/DoTrack.GitProviders.GitHub/GitHubAdapter.cs`
- SPA page (read-only): `frontend/src/pages/WorkItemDetailPage.tsx`
- SPA route wiring: `frontend/src/router.tsx` (code-based, nested under `workspaceRoute`)
- API client singletons: `frontend/src/lib/api.ts`

`docs/CONVENTIONS.md` walks the templates.

## Repository state

`SESSION_LOG.md` lists every phase committed so far. `README.md` documents the API surface that's online.
