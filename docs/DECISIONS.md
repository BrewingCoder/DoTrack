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

## Frontend stack (deferred)

When frontend work starts: React + shadcn/ui + Tailwind + TipTap + Vaul. Mobile-first PWA. Path A — desktop primary, intentional mobile variants for triage / comment / my-work / mobile board. No JVM languages.

## Authentication

Deferred. `ICurrentUserAccessor` exists; `NullCurrentUserAccessor` returns null in v0. OIDC + local accounts wire up later.

## Force-pushes to `main`

Two ever, both early in v0 baseline (commit-author email correction, closure-table FK fix). The branch is now stable; treat history as read-only.
