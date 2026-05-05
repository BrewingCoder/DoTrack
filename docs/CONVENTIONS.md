# Conventions

## Use case template

When adding a new use case (an endpoint or background operation that mutates a domain entity):

1. **Domain method** on the entity for the state change. Bumps `UpdatedAt`. Throws `ArgumentException` / `ArgumentOutOfRangeException` for invalid input.
2. **Command + Result records** + handler interface in `DoTrack.Application/<Area>/`.
3. **Handler implementation** in `DoTrack.Infrastructure/<Area>/`. Constructor takes `DoTrackDbContext`, `TimeProvider`, optionally `OutboxEmitter`.
4. **Register** in `DoTrack.Infrastructure/DependencyInjection.cs` as `AddScoped`.
5. **Multi-provider integration tests** in `DoTrack.Infrastructure.Tests/<Area>/` via `DatabaseTestBase<TFixture>` + three concrete derivations.
6. **Audit assertion** in tests: every persisted change produces correct audit rows; `[NotAudited]`-only changes shouldn't audit.

Reference: `CreateWorkItem` end-to-end.

## Endpoint template

1. **Request + Response DTOs** in `DoTrack.Api/<Area>/Contracts.cs`. Wire types are `Guid` / strings / primitives — not strongly-typed IDs.
2. **Validator** in `DoTrack.Api/<Area>/<X>RequestValidator.cs`. Static method, returns `IDictionary<string, string[]>?`. Null when valid; populated dictionary is piped to `Results.ValidationProblem`.
3. **Mapper** static helper for DTO ↔ command/domain.
4. **Endpoint method** on a static extension class (`MapXEndpoints`).
5. **Wire** in `Program.cs`.
6. **HTTP test class** in `DoTrack.Integration.Tests/<Area>/`, `[Collection(nameof(IntegrationCollection))]`.

Reference: `WorkItemEndpoints.cs`.

## PATCH semantics

PATCH endpoints use null-means-no-change. If a request omits a field or sends it as null, the field stays unchanged. Explicit clear/unassign requires a dedicated endpoint (e.g., `DELETE /work-items/{n}/sprint`).

## Validation

Manual validators (no FluentValidation). Static method, returns `IDictionary<string, string[]>?`. Null when valid. Endpoint:

```csharp
var errors = SomeRequestValidator.Validate(body);
if (errors is not null) return Results.ValidationProblem(errors);
```

## Errors

| Status | When |
|---|---|
| 400 | Validation failure → `ValidationProblemDetails` |
| 404 | Domain entity not found |
| 409 | Uniqueness conflict (slug, key, email) |
| 500 | Unhandled (shaped via `app.UseExceptionHandler()` + `AddProblemDetails()`) |

`Results.Problem(statusCode: ..., title: ..., detail: ...)` everywhere a non-validation problem is returned.

## JSON

- Camel-case property names (`PropertyNamingPolicy = JsonNamingPolicy.CamelCase`).
- Enums serialise as strings (`JsonStringEnumConverter`).
- `DateTimeOffset` ISO-8601.

Tests use the same `ApiJsonOptions.Default` so wire format is verified end-to-end.

## Strongly-typed IDs

Domain entities use `readonly record struct XId(Guid Value)`. EF Core converts via `ConfigureConventions` with private nested `ValueConverter<XId, Guid>` classes in `DoTrackDbContext`. DTOs expose `Guid`, never `XId` — wire format is C#-agnostic.

## Outbox emission

Inject `OutboxEmitter` into handlers that should emit automation events. Call `outbox.Emit(eventType, projectKey, payload)` BEFORE `SaveChangesAsync` so the outbox row commits in the same transaction as the domain change.

Event names use `noun.verb` form: `issue.created`, `issue.state_changed`, `issue.assigned`, `issue.commented`, `time.logged`.

## Commit style

Conventional Commits. `feat(area): summary` / `chore: summary` / `fix(area): summary`. Body wraps at ~78. Always include a `Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>` trailer when committed by Claude.

Stage by name (`git add path/...`) rather than `git add -A` to avoid accidentally including secrets or large binaries.

## Code style

- File-scoped namespaces (`namespace X;`)
- Primary constructors where natural
- `sealed class` by default
- `var` when type is apparent, explicit otherwise
- Treat warnings as errors in production code; tests have `TreatWarningsAsErrors=false`
- All analyzer suppressions in `Directory.Build.props` are documented inline next to the rule

## Migration generation

```sh
dotnet ef migrations add X --project src/DoTrack.Migrations.<Provider> \
  --startup-project src/DoTrack.Migrations.<Provider> --output-dir Migrations
dotnet ef database update --project src/DoTrack.Migrations.<Provider> \
  --startup-project src/DoTrack.Migrations.<Provider>
```

When a model change requires a new migration, generate it for all three providers in the same phase. Don't ship a model change with only one provider's migration applied.

## Adding a new entity to test cleanup

When you add a new persisted entity:

1. Update `DatabaseTestBase<TFixture>` cleanup to delete the new table in FK-safe order.
2. Update `DoTrackApiFactory.ResetDataAsync` to delete the same table.

Tests fail fast when this is missed: rows leak between tests and assertions on counts break.

## Wiring an endpoint into the SPA

When the SPA needs to consume an existing endpoint:

1. **Annotate the endpoint** with `.Produces<TResponse>(StatusCodes.Status200OK)` so the OpenAPI spec carries the response schema. Without this, NSwag emits `void` / `any`. Path A annotation strategy — annotate as the UI consumes, not preemptively.
2. **Restart the API** so the spec reflects the change.
3. **Regenerate the TS client**: `cd frontend && pnpm gen:api`. Commit the regenerated `frontend/src/api/generated.ts` with the change.
4. **Add a client singleton** in `frontend/src/lib/api.ts` if the new tag's client class isn't already exposed.
5. **Wrap calls in TanStack Query** (`useQuery` / `useMutation`) inside the page component. Use a stable `queryKey` of `[entity, ...path-params]`.

## Frontend page template

When adding a new page:

1. **Create the component** under `frontend/src/pages/`. Use the layout shell automatically (it wraps everything via the rootRoute).
2. **Register the route** in `frontend/src/router.tsx`. Nest under `workspaceRoute` if it operates within a workspace context (uses `$wsSlug`).
3. **Read route params** via `useParams({ from: '/route/path' })` for type-safe access.
4. **Reuse shadcn primitives** from `frontend/src/components/ui/`. Run `pnpm dlx shadcn@latest add <name>` to install new ones; they're checked into source.
5. **Reference implementation:** `frontend/src/pages/WorkItemDetailPage.tsx` shows the full pattern (multiple chained queries, tabs, sidebar metadata, loading/error/empty states).
