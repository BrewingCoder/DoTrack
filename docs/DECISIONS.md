# Decisions

Locked architectural decisions. New decisions append to the list. Reasoning beyond technical mechanism is intentionally absent from this document.

## Multi-provider EF Core

PostgreSQL, SQL Server, and SQLite are first-class. MySQL is deferred until Pomelo ships an EF Core 10 build. Migration assemblies are per-provider; the runtime provider is selected via `Database:Provider` config.

## Strongly-typed IDs at the domain layer

Domain entities use `readonly record struct XId(Guid Value)`. DTOs expose `Guid`. EF Core converts via global conventions in `DoTrackDbContext.ConfigureConventions`.

## Audit log shape

Central `audit_logs` table with structured field-diff JSON, captured via `AuditingInterceptor : SaveChangesInterceptor`. Per row: entity type, entity ID, change type, changed-by user (nullable), occurred-at, source, change reason, source metadata, field-changes array.

## Audit exclusions

- `User` entity — class-level `[NotAudited]`.
- `Project.NextWorkItemNumber` — property-level `[NotAudited]`.
- `OutboxMessage` — class-level `[NotAudited]`.

The `[NotAudited]` attribute supports both class- and property-level opt-out. Apply at property level when only one field on an audited entity should be quiet.

## Closure-table FK behaviour

`work_item_hierarchy` foreign keys to `work_items` use `OnDelete(NoAction)`. SQL Server rejects multiple cascade paths to the same table, so the same pattern applies to:

- `work_item_links` (both source and target FKs)
- `WorkItem.SprintId` (Project → Sprint → WorkItem.SprintId vs Project → WorkItem.ProjectId direct)
- `MilestoneScope.MilestoneId`
- `SavedQuery.ProjectId`

App code removes the join rows before deleting the related entity.

## SQLite — `OrderBy DateTimeOffset`

SQLite cannot `ORDER BY` a `DateTimeOffset` column server-side. Handlers that order by `DateTimeOffset` materialise via `ToListAsync`, then sort client-side. Applied uniformly across providers.

Future: a monotonic `Sequence` column on `audit_logs`, `comments`, `time_entries` replaces the client-side sort when high-volume use forces it.

## SQLite — VARCHAR length not enforced

SQLite does not enforce `VARCHAR(N)` length constraints. Tests that depend on max-length rejection guard with `Fixture.ProviderName == "Sqlite"` and `Assert.Skip(...)`.

## PATCH null-means-no-change

PATCH endpoints treat null/missing fields as "leave unchanged." Clearing a field requires a dedicated endpoint. The trade-off is a simpler wire format at the cost of an extra endpoint per clearable field.

## State machine

Free-form transitions for v0. State changes are accepted regardless of current state; the audit row captures the full diff. Configurable per-project workflow rules ship in v1.

## Tier rules for parent-child

- `Epic → Feature` ✓ (cross-project allowed)
- `Epic → Item` ✓ (same project only)
- `Feature → Item` ✓ (same project only)
- Anything else: rejected.
- Cross-project parent-child links: only at the `Epic → Feature` boundary.

`Item` is the leaf tier — it cannot have descendants.

## Sprint membership

Only `Item`-tier work items can be assigned to a sprint. `Epic` and `Feature` represent scope rather than sprint work. The domain layer rejects assignment of non-Item tiers.

## Outbox transactional emission

`OutboxEmitter.Emit(...)` is called before `SaveChangesAsync` so the outbox row commits in the same transaction as the domain change. Background `OutboxDispatcherService` polls and delivers via the registered `IAutomationProvider`. At-least-once delivery semantics; idempotency is the consumer's responsibility (event IDs are durable and unique).

## Internal comments don't emit automation events

`AddCommentHandler` emits `issue.commented` only when `IsInternal == false`. Internal comments stay off-wire by default to match the visibility posture.

## Auto-comment author

Webhook-driven auto-comments use the work item's `ReporterId` as the comment author. This is a v0 stand-in until authentication lands and per-commit-author lookup or a dedicated "system" user replaces it.

## ChangedByUserId nullable for system actions

Audit rows from system actions (no HTTP context, no `ICurrentUserAccessor` resolution) record `ChangedByUserId = null`. The schema permits nulls; UI distinguishes "system" from real users.

## Test library set

xUnit.v3 + Shouldly + NSubstitute + Bogus + Verify.XunitV3. AwesomeAssertions and Moq are not used.

## Test naming

`Method_Scenario_ExpectedBehavior`. CA1707 suppressed for test projects.

## Test fixture sharing

HTTP integration tests share one `DoTrackApiFactory` per assembly via `[Collection(nameof(IntegrationCollection))]`. Per-class factories collide on the implicit `Program` type.

## Validation library

Manual validators. Static method, returns `IDictionary<string, string[]>?`. Endpoints pipe to `Results.ValidationProblem`.

## Frontend stack

Vite + React + TypeScript + Tailwind v4 + shadcn/ui (Nova preset, Geist font). TanStack Query 5 for fetch state, TanStack Router 1 (code-based) for routing. Path A — desktop primary, intentional mobile variants for triage / comment / my-work / mobile board. PWA shell ships when a feature needs it (TipTap, Vaul). No JVM languages.

Frontend lives at `frontend/` in the repo root, not under `src/` (which is reserved for .NET projects).

## Frontend dev port

Vite binds `127.0.0.1:5273` (not the Vite default 5173, which clashes with another project on this machine). `strictPort: true` so collisions fail loudly instead of silently shifting. The dev API CORS rule and the Vite proxy both use 5273.

## Vite dev proxy = same-origin frontend↔API

In dev, `frontend/vite.config.ts` proxies `/api`, `/healthz`, `/openapi` to `http://localhost:5259`. The SPA uses relative URLs (empty baseUrl on the generated NSwag clients). Same-origin in dev means no cross-origin CORS gymnastics; in prod, the same paths are served by the same reverse proxy that serves the SPA.

The dev-only CORS policy in `Program.cs` is technically dead code now — kept as a safety net if the proxy is removed.

## OpenAPI client generation — NSwag, on-demand

`pnpm gen:api` regenerates `frontend/src/api/generated.ts` from the running API at `http://localhost:5259/openapi/v1.json`. Configured for Fetch template, Interface type style, StringLiteral enums, `dateTimeType: "string"` (DateTimeOffset wire format honesty), `MultipleClientsFromFirstTagAndOperationId` so each tag becomes its own client class.

Spec generation stays on `.NET-native AddOpenApi` (`Microsoft.AspNetCore.OpenApi`); Swashbuckle is in only for the SwaggerUI shell at `/swagger`.

## Path A — annotate `.Produces<T>(200)` per endpoint as the UI consumes it

Endpoints currently use `IResult` returns (`Results.Ok(...)`). The .NET-native OpenAPI generator can only infer response schemas from `TypedResults.Ok<T>(...)` or explicit `.Produces<T>(200)` registrations. Without one, NSwag emits `void`/`any` returns and the codegen pipeline loses its value.

Decision: annotate endpoints with `.Produces<TResponse>(200)` as the UI consumes them, rather than refactoring all ~30 endpoints to `TypedResults.Ok<T>` up front. New UI features carry their annotations forward. Eventually the full TypedResults refactor lands as its own phase.

## DbContext config resolved inside the factory delegate

`AddConfiguredDatabase` resolves `Database:Provider` and `Database:ConnectionString` from `IServiceProvider.GetRequiredService<IConfiguration>()` *inside* the `AddDbContext` factory delegate, not at registration time.

Reading at registration time captured `appsettings.Development.json`'s value before `WebApplicationFactory.ConfigureAppConfiguration` overrides could be applied — every integration test silently ran against the developer's dev rig DB instead of its testcontainer. Reading inside the factory delegate ensures the test harness's InMemoryCollection wins.

`DoTrackApiFactory.InitializeAsync` carries a regression guard that asserts the resolved DbContext is bound to `dotrack_integration`. Reverting to eager registration fails loudly on every test run.

## Authentication

Deferred. `ICurrentUserAccessor` exists; `NullCurrentUserAccessor` returns null in v0. OIDC + local accounts wire up later.

## Force-pushes to `main`

Two ever, both early in v0 baseline (commit-author email correction, closure-table FK fix). The branch is now stable; treat history as read-only.
