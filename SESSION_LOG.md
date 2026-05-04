# Overnight session log — May 3-4 2026

Autonomous build session. ~340 tests passing across PG / SqlServer / SQLite at session end. Each entry below is one git commit.

## Foundation (pre-autonomous, on-the-call)
- **Initial scaffold** — .NET 10 solution, 21 projects, EF Core multi-provider migrations, v0 domain model
- **Test stack** — xunit.v3, Shouldly, NSubstitute, Bogus, Verify.XunitV3 (no AwesomeAssertions, no Moq)
- **Audit infrastructure** — central `audit_logs` table, EF SaveChanges interceptor, `[NotAudited]` attribute opt-out, ports for current user + audit context
- **Multi-provider integration test harness** — PG/SqlServer/SQLite via Testcontainers, every infrastructure test runs against all three
- **Provider-aware Api wiring** — `Database:Provider` config selects EF provider with right MigrationsAssembly
- **WorkItem REST endpoints + DTOs + validation + integration tests via WebApplicationFactory** — established the API pattern that every later resource follows

## Autonomous run (Phases A-S)

| Phase | Commit | What shipped |
|---|---|---|
| **A** | `d07a1a0` | UpdateWorkItem PATCH endpoint with state transitions (PATCH semantics: null = no change) |
| **B** | `2875644` | SetParent + closure-table maintenance + tier rules (Epic→Feature/Item, Feature→Item, cross-project Epic→Feature only) + cycle prevention |
| **C** | `40fec2e` | Comments — internal vs external visibility, default list excludes internal |
| **D** | `8592cd1` | TimeEntries — DCAA-aligned (positive duration, non-empty description, ≤24h) |
| **E** | `f756d15` | GitHubAdapter — HMAC signature verification, push + pull_request canonical events |
| **F** | `9f83351` | Work item history endpoint — queries audit_logs by entity |
| **G** | `ff4b05e` | AcceptanceCriteria — Pending / Met / Waived (waiver requires reason) |
| **H** | `4cf08b7` | Sprints — project-scoped, only Items can be assigned, sprint deletion clears assignments |
| **I** | `7ef8579` | Bootstrap endpoints — workspaces / projects / users CRUD so the system can be set up purely from API |
| **J** | `f25be7d` | Milestones — cross-project, scope membership, computed health math (budgetPct, scopePct, healthGap, projectedTotal, projectedOverage) |
| **K** | `36f01f4` | Issue links — Blocks / Duplicates / Causes / Relates with bidirectional listing |
| **L** | `0216365` | Saved queries — Personal / Project / Public scopes (parser is a future phase) |
| **M** | `6eca388` | Smart-commit dispatcher — webhooks actually update work items (#fixed→Accepted, #in-progress→InProgress, auto-comment on every commit/PR with an issue key) |
| **N** | `e2061f1` | Outbox + N8nAutomationProvider + OutboxDispatcherService BackgroundService — at-least-once delivery to n8n with HMAC |
| **O** | `7d7e322` | Watchers + My Work feed (assigned + reporting + watching, all non-Accepted) |
| **P** | `3b511d0` | Expanded outbox emission — issue.state_changed, issue.assigned, issue.commented, time.logged |
| **Q** | `861cc8d` | Polish — README rewrite, root docker-compose + Dockerfile, GitHub Actions CI |
| **R** | `d57394c` | Gitea/Forgejo adapter + webhook receiver |
| **S** | `43a0981` | Bitbucket Cloud adapter + webhook receiver — Git provider trio complete |

## Where v0 stands at end of session

**Working backend** with HTTP API covering: bootstrap, the four-tier work item hierarchy with closure table, monotonic per-project numbering, comments with visibility, time entries, audit log, acceptance criteria, sprints, milestones with health math, watchers, saved queries, full Git provider trio (GitHub / Gitea / Bitbucket) with smart-commit automation, transactional outbox + n8n outbound delivery.

Multi-provider matrix proven: every infrastructure test runs against Postgres + SqlServer + SQLite. One SQLite-specific skip (VARCHAR length isn't enforced, documented).

## What's deliberately not built yet

- **Authentication / OIDC** — `ICurrentUserAccessor` returns null today; auto-comments and audit ChangedByUserId are null/reporter as a stand-in. Auth lands as its own phase with OIDC provider config.
- **Email notifications** — outbox + n8n is the automation channel; in-product email needs SMTP setup.
- **Email-in for issue creation** — the JIRA SM wedge feature, deferred.
- **Query language parser** — saved queries store the string; the recursive-descent parser is reserved (`DoTrack.QueryLanguage` project exists, empty for now).
- **Custom fields** — design has it, implementation deferred.
- **Configurable per-project workflow** — state transitions are free-form for v0.
- **Frontend** — not started; held until UX-reference rig (YouTrack at `:8888`) is populated.

## Known TODO / production concerns recorded inline

- **SQLite + DateTimeOffset ORDER BY** — handlers sort client-side; production should add a monotonic `Sequence` column on audit_logs / comments / time entries before high-volume use.
- **Outbox dispatcher** — synchronous fire-and-forget could be added as a fast path; current design is "always go through the outbox + background worker."
- **Project key collision globally** — IssueKeyDetector.FindByIssueKeyHandler picks first-by-CreatedAt when keys collide across workspaces. Real installations need webhook-config → repo → workspace mapping to scope this.
- **Force-push rule** — only two force-pushes ever happened, both early in v0 baseline (commit message correction + closure-table FK fix). Branch is now stable; treat history as read-only.
