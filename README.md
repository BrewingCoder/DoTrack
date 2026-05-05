# DoTrack

A self-hosted, OSS issue tracker and project manager designed for small dev teams running T&M engagements with active client/stakeholder involvement.

> Looks like JIRA, weighs like Vikunja, treats clients as first-class participants without per-seat pricing.

**Status:** v0 — backend API surface online. Read-only SPA scaffold (workspaces / projects / work items / detail) live alongside the YouTrack UX-reference rig. Mutations and remaining pages still to come.

## Stack

- **Backend:** .NET 10 + EF Core (multi-provider: PostgreSQL / SQL Server / SQLite; MySQL deferred until Pomelo ships EF Core 10)
- **Frontend:** Vite + React 19 + TypeScript + Tailwind v4 + shadcn/ui (Nova preset). Visual language emulates JetBrains YouTrack 2026.x — dark canvas, 200px left navigation rail, system-ui at 14px. TanStack Query for fetch state, TanStack Router for routing. NSwag for typed TS clients generated against the running API.
- **Distribution:** Docker Compose at root for full stack; Helm chart later
- **License:** Apache 2.0

## What's in v0 (running on backend)

| Area | Endpoints |
|---|---|
| Bootstrap | POST/GET workspaces, projects, users |
| Work items | POST/GET/PATCH; PATCH transitions state; closure-table parent links; issue-link relations (Blocks/Duplicates/Causes/Relates); watchers; My Work feed |
| Per-project sequencing | Monotonic per-project numbers (PROJ-42 style), backed by Project.NextWorkItemNumber with `[NotAudited]` so allocation doesn't audit-spam |
| Acceptance criteria | Add/list/update status (Pending/Met/Waived) with required reason on waive |
| Sprints | Project-scoped, work item assignment (Items only — Epics/Features rejected), sprint-deletion clears assignments |
| Milestones | Cross-project scope membership with computed health (budgetPct, scopePct, healthGap, projectedTotal, projectedOverage) |
| Comments | Internal vs external visibility, default list excludes internal |
| Time entries | DCAA-aligned: positive duration, non-empty description, ≤24h per entry |
| Audit log | Every domain change captured (User excluded — privacy posture). `[NotAudited]` property reservation lets us mark e.g. NextWorkItemNumber out of the diffs. History endpoint per work item. |
| Saved queries | Personal/Project/Public scopes — query string stored verbatim, parser is a future phase |
| Git integration | GitHubAdapter implements IGitProviderAdapter with HMAC-SHA256 signature verification + push/pull_request parsing. Webhook receiver dispatches smart-commit directives (#fixed, #resolved, #closed, #in-progress) to transition referenced work items, and adds auto-comments for every commit/PR linking to a key |
| Automation outbox | Transactional outbox + background OutboxDispatcherService; N8nAutomationProvider posts JSON+HMAC to a configured webhook. Producers: issue.created, issue.state_changed, issue.assigned, issue.commented, time.logged |

Multi-provider matrix proven by integration tests across Postgres / SQL Server (Rosetta) / SQLite Testcontainers fixtures. ~330 tests at the time of this README.

## Repository layout

```
src/
  DoTrack.Domain/                       Pure domain model, no deps
  DoTrack.Application/                  Use cases, ports
  DoTrack.Infrastructure/               EF Core, audit interceptor, handlers, outbox emitter
  DoTrack.Migrations.{Postgres,SqlServer,Sqlite}/
  DoTrack.QueryLanguage/                Reserved — parser ships in v0.x
  DoTrack.GitProviders.{Abstractions,GitHub,Gitea,Bitbucket}/
  DoTrack.Automation.{Abstractions,N8n}/
  DoTrack.Workers/                      OutboxDispatcherService (BackgroundService)
  DoTrack.Api/                          ASP.NET Core composition root
tests/
  DoTrack.{Domain,Application,Infrastructure,QueryLanguage,GitProviders,Integration}.Tests/
frontend/
  src/api/generated.ts NSwag-generated typed TS client (regenerated via `pnpm gen:api`)
  src/components/      Layout shell + shadcn/ui components
  src/lib/             API client singletons + QueryClient
  src/pages/           Page components (ProjectsPage, WorkItemsPage, WorkItemDetailPage)
  src/router.tsx       TanStack Router code-based route tree
.dev/
  docker-compose.yml   Umbrella `dotrack` project (Postgres + YouTrack)
  youtrack-ref/        Local YouTrack container (UX reference rig)
  postgres-dev/        Local Postgres for migrations + dev runs
  sqlserver-dev/       Local SQL Server (amd64 under Rosetta on AIRM5)
docker/
  docker-compose.yml   Production-like local stack (web + Postgres)
.github/workflows/
  ci.yml               Build + test on PR/push
```

## Build

```sh
dotnet restore
dotnet build
dotnet test
```

## Run locally

Start the dev rig (Postgres + YouTrack reference) under one Docker project named `dotrack`:

```sh
docker compose -f .dev/docker-compose.yml up -d
```

Run the API:

```sh
dotnet run --project src/DoTrack.Api --launch-profile http
```

The API listens on `http://localhost:5259`:
- OpenAPI doc: `/openapi/v1.json`
- Swagger UI: `/swagger` (dev only)

Run the frontend (in another terminal):

```sh
cd frontend
pnpm install            # first time only
pnpm dev                # http://localhost:5273
```

The Vite dev server proxies `/api`, `/healthz`, and `/openapi` to the API, so the SPA is same-origin in dev.

Bootstrap with the API directly:

```sh
curl -X POST http://localhost:5259/api/v1/workspaces -H 'Content-Type: application/json' \
  -d '{"name":"Acme","slug":"acme"}'
curl -X POST http://localhost:5259/api/v1/workspaces/acme/projects -H 'Content-Type: application/json' \
  -d '{"key":"PROJ","name":"First Project"}'
curl -X POST http://localhost:5259/api/v1/users -H 'Content-Type: application/json' \
  -d '{"email":"alice@example.com","displayName":"Alice"}'
```

After adding/changing endpoints the SPA consumes, regenerate the typed client:

```sh
cd frontend && pnpm gen:api
```

## Configuration

Required:
- `Database:Provider` — `postgres` | `sqlserver` | `sqlite`
- `Database:ConnectionString`

Optional:
- `Webhooks:GitHub:Secret` — HMAC verification on the GitHub webhook receiver
- `Automation:N8n:WebhookUrl` — when set, the OutboxDispatcherService is registered and posts events here
- `Automation:N8n:Secret` — HMAC for outbound n8n delivery
