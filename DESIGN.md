# Project Handoff — Issue Tracker / Project Manager

*A self-hosted, OSS issue tracker and project manager designed for small dev teams running T&M engagements with active client/stakeholder involvement. Targets the gap between toy-grade lightweight tools (Vikunja, Plane, Kanboard) and enterprise-heavyweight platforms (JIRA, YouTrack, OpenProject).*

---

## The Pitch

> A self-hosted issue tracker that **looks like JIRA, weighs like Vikunja, and treats clients as first-class participants without per-seat pricing.**

**The wedge:** small DC-area consultancies, agencies, primes, and subs running federal-adjacent T&M engagements need a tool their clients can log into and trust on first sight. JIRA is overkill and per-seat-priced. YouTrack gets close on functionality but looks dated. Lightweight OSS options look like weekend projects. Nothing in the middle exists.

**The differentiators:**

1. **4-tier work hierarchy with real rollup** — Epic → Feature → Work Item → Time Entry. Activity, time, commits, and acceptance criteria all genuinely roll up the chain. JIRA fakes this with epic-link plugins; ADO fakes it with cosmetic counts; GitLab makes you simulate it with labels; YouTrack gets closest but rollup is plugin-flavored.
2. **Sprints AND Milestones as orthogonal axes**, not conflated containers. Sprints are operational/team-facing. Milestones are executive/budget-facing. Different audiences, different reports.
3. **YouTrack-class query language** with autocomplete, persisted views, and matrix boards (any field × any field).
4. **Federal-T&M-aware** by design: DCAA-flavored timekeeping, audit trails, scope-vs-budget reconciliation reports, internal vs client-visible comments, approver roles.
5. **First-class self-hosted Git provider support** including Gitea/Forgejo (which YouTrack famously breaks).
6. **n8n integration as the automation layer** — no in-product workflow engine to maintain. Outsource what's already done well.
7. **Apache 2.0 with trademark protection** — the trust wedge for federal-adjacent buyers.

---

## Core Constraints (Non-Negotiable)

- **2 containers maximum for the core ship** (web + DB). Everything else is bolt-on.
- **DB-flexible:** SQL Server, Azure SQL, Postgres, MySQL, SQLite. Driven by EF Core provider abstraction.
- **Self-hosted first.** Docker Compose + Helm chart distribution.
- **OSS:** Apache 2.0, DCO for contributions, plugin model for monetization.
- **Mobile-first responsive PWA.** Not a separate native app.
- **Enterprise visual gravitas.** Stakeholders log in and see "real software," not a sofa project.
- **No vendor lock-in.** Every install is portable, exportable, archivable.

---

## Stack

### Backend

- **.NET (latest LTS) + EF Core** — for DB provider flexibility and a strong typed query story
- **Hand-rolled query language parser** (recursive descent, ~500 lines) → AST → EF Core LINQ expression tree
- **Background queue:** DB-backed (advisory locks on Postgres, equivalent on SQL Server) — no Redis required for v1
- **Email delivery:** SMTP-configurable, pluggable provider abstraction (SendGrid/SES/Postmark as drop-in alternatives)

### Frontend

- **React + shadcn/ui + Tailwind**
- **TipTap (ProseMirror)** for rich text editing
- **TanStack Query** for data fetching with optimistic updates
- **TanStack Table** for dense data grids with virtualization
- **cmdk** for the global command palette
- **dnd-kit** for board drag-and-drop
- **Recharts** for dashboard charts
- **PWA from day one** — manifest, service worker for shell caching, web push

### Distribution

- **Docker Compose** for the canonical install (`docker-compose.yml` for core, `docker-compose.with-n8n.yml` for the optional automation layer)
- **Helm chart** for cloud self-hosting
- **GHCR** for signed container images
- **SBOM published with each release** (Syft or equivalent)
- **Sigstore-signed releases**

---

## Identity & Access

### Auth Model

- **Local accounts always available** (admin bootstrap, air-gapped installs, small-team installs)
- **OIDC providers configurable per install** — GitHub, Microsoft (Entra), Google, Okta, Auth0, Authentik, Keycloak, anything OIDC-compliant
- **Multiple providers simultaneously.** Login screen shows configured buttons.
- **Per-provider config:** custom label ("Sign in with Acme SSO"), default role on first login, optional domain restriction.
- **No SAML in v1.** OIDC covers 95% of modern IdPs. Defer SAML until a buyer demands it.
- **No SCIM in v1.** Not the target buyer's concern.

### Account Identity Rules

- **Email is the unique key.** One account per verified email, full stop.
- **First-time OIDC login that collides with existing email:** "You already have an account with that email. Sign in with your existing method to link this provider." No silent duplicates, no admin pre-provisioning required.
- **Internal linkage by `sub` claim, not email.** Provider linkage survives email changes at the IdP.
- **Account uniqueness enforced at initiation AND verification** — race conditions handled.

### Email Change Flow

- New email entered → account email **does not change yet**
- Verification email sent to new address (24h token)
- Notification email sent to old address ("if this wasn't you, click to cancel")
- On verification: email officially changes, all sessions invalidated
- Admin-initiated changes follow the same flow — no bypass, no "admin reset email to attacker" attack
- Recent-auth check (re-prompt for password if session > 15 min old) for sensitive actions

### Workspaces & Projects

- **Workspaces** group projects by client/engagement
- **Per-workspace branding:** logo, primary color, custom subdomain or path, custom email "from" name
- **Internal staff see all workspaces** they're members of
- **Client users are workspace-scoped** and cannot see other workspaces exist
- Projects belong to workspaces; permissions cascade workspace → project → role

### Roles

- **Admin** — full system control
- **Maintainer** — workspace/project admin
- **Developer** — full project access including dev actions (create branch, etc.)
- **Reporter** — file and comment on issues
- **Client** — workspace-scoped, file/comment/review on assigned projects only, internal-comment-blind
- **Viewer** — read-only
- **Approver** (special, milestone-scoped) — only role permitted to sign off milestone completion

---

## Work Hierarchy

### The Four Tiers

```
Epic
 └─ Feature
     └─ Work Item (Story | Bug | Task | Spike | Chore)
         └─ Time Entry
```

- **Tier names renamable per project** (e.g., "Initiative" / "Module" / "Item")
- **Work Item types** are configurable per project — type lives at this tier, not at the structural level
- **Time Entry tier name is not user-renamable** — it's a structural primitive

### Hierarchy Rules

- **Time entries attach to Features or Work Items, never directly to Epics.** Epics are scope, not work. Logging time on an Epic skips the question "what specifically did you work on?" Forces honest rollup.
- **Loose orphan handling.** Work Items can exist without a Feature, Features can exist without an Epic. Synthetic per-project "Unassigned" Feature and Epic catch orphans for rollup math. UI nags users to assign upward without forcing it.
- **Cross-project linking allowed only at the Epic→Feature boundary.** Features and below are project-bounded. Handles cross-team initiatives without exploding the permission/query model.

### Rollup Contract

**Up the chain (work performed rolls uphill):**

- Time entries roll up: Feature shows sum of descendant Work Item time; Epic shows sum of all descendant Feature + Work Item time
- Commits/PRs/branches roll up: Epic view shows every Git event linked to any descendant
- Status/progress rolls up: Epic's "% complete" is computed from descendants
- Activity feed rolls up: chronological stream of every comment, status change, and time entry from anything below
- Acceptance criteria roll up: Epic shows "X / Y criteria met across all descendants"

**Down the chain (context flows downhill):**

- Work Item knows its Feature and Epic without manual joins
- Permissions cascade workspace → project → role; subtree restrictions possible but rare

### Implementation

- **Closure table** for hierarchy (cheap descendant queries)
- **Materialized rollup columns** on Epic/Feature for time totals, completion counts, etc.
- **Updated via app-layer domain events** on time entry / status change
- For 2–25 user installs, this is fast and simple. Closure table + materialized columns is the right pattern.

### Linked Issues / Dependencies

Lateral relationships beyond hierarchy:

- Blocks / Blocked by
- Duplicates / Duplicated by
- Related to
- Causes / Caused by
- Custom link types per project

Dependency-aware sprint planning ("can't pull in until that's done"), critical path visualization, "this issue is blocking N others" warnings.

---

## Sprints & Milestones (Orthogonal Axes)

### Sprints — for the team

- **Project-scoped, NOT cross-project**
- Fixed cadence (1–4 weeks typically)
- Stakeholder-visible by default — clients see sprint progress in real-time
- Hold Work Items only (Features/Epics show ancestry context on cards but aren't *in* sprints)
- One active sprint per project as default; warn on second active sprint, don't forbid
- Sprint board with kanban + matrix variants
- Velocity, burndown, sprint review reports

### Milestones — for the # crunchers

- **Cross-project allowed**
- Defined by `target_date` AND/OR `hours_budget` + explicit scope list
- Auto-include-descendants option when adding Epics/Features to scope, with explicit exclude-specific-items override
- No board, no kanban — milestones are *reports*, not workspaces
- Per-milestone client visibility toggle (often contains budget figures)
- Snapshot weekly for trajectory visualization

### Milestone as Conversation Tool

This is the signature feature. Milestones exist to surface the "we need to talk" moment with data:

> *"We're at 90% of the estimated budget but only 60% of the scope is done. Here's the data. Drop scope, raise budget, or accept a slip."*

**Computed reconciliation options on every milestone view:**

```
At current pace:
  • Estimated total: 300h (vs 200h budget)
  • Projected completion: Sept 18 (vs Aug 30 target)

Reconciliation options:
  • Drop ~6 scope items to fit budget, OR
  • Add ~100h to budget, OR
  • Accept 19-day slip
```

**Math:**

```
budget_pct       = hours_logged / budget_hours
scope_pct        = scope_done / scope_total
health_gap       = budget_pct - scope_pct
projected_total  = hours_logged / scope_pct
projected_overage = projected_total - budget_hours
items_to_drop    = ceil(projected_overage / hours_per_done_item)
budget_increase  = projected_overage
slip_days        = (estimated_remaining / recent_burn_rate) - days_until_target
```

**Health states:**

- 🟢 On track: `health_gap` ±10%, projected within budget AND date
- 🟡 Warning: `health_gap` >10%, OR projected miss <20%, OR slip <2 weeks
- 🔴 At risk: `health_gap` >25%, OR projected overage >20%, OR slip >2 weeks
- ⚫ Conversation overdue: at risk for 14+ days with no scope/budget adjustment

**Scope change audit trail with reason field** — the contractual paper trail. When the client picks "drop 6 items," every removal is logged with who, when, why.

### Permissions Interaction

- Sprints stakeholder-visible by default
- Milestones admin/PM-visible by default; per-milestone toggle for client visibility (when budget transparency is wanted)

---

## Estimation & Time Tracking

### Estimation

- **Both points and hours, both first-class, project-configurable**
- `estimation_mode`: points | hours | both
- `velocity_unit`: points | hours (drives the velocity chart; the other shows as secondary number)
- Default scale: Fibonacci (1/2/3/5/8/13/21); configurable per project (t-shirt sizes, linear, custom)
- Manual estimates at Feature/Epic level, displayed alongside rollup-of-descendants estimates for honest comparison

### "Feel Like a Cap" Visualization

The federal-T&M client psychological need: a number that looks like a budget without contractually being one.

- Sprint shows: *"Estimated: 80h | Logged: 58h | Remaining: 22h estimated"* with progress bar
- Milestone shows: *"Budget: 200h | Logged: 142h | Burn rate: 8h/day | Projected: 218h"*
- Language is **"estimated"** and **"projected,"** never **"cap"** or **"limit"**
- **Estimates are NEVER enforced as caps.** Logging time always works. Reality wins.
- Per-project setting: "Show estimate vs actual to clients"

### Time Tracking

```
TimeEntry
├─ user_id
├─ work_item_id (or feature_id)
├─ started_at, ended_at (or duration)
├─ description (narrative — required for federal billing)
├─ billable (bool)
├─ activity_type (configurable per project: Development, Code Review, Testing, etc.)
├─ created_at (immutable)
└─ last_edited_at
```

- Editable by the logger; admin can edit any
- Both start/stop timer AND manual entry supported

### DCAA-Flavored Behaviors

For small federal contractors, lightweight compliant timekeeping:

- Time entered daily by the person who did the work
- Edits to entries older than N days (default 7) require a reason field
- Reason + before/after values logged to immutable audit log
- Exports include edit history, not just current values
- Period locking: once exported for invoice period X, entries are locked from edit (override available with audit trail)
- Optional rate cards per project (assign labor rates to users so export can include $ totals)

### Invoice Export

- Configurable CSV/Excel templates per project
- Fields: user, labor category, date, hours, work item, narrative, billable, optional rate, optional total
- Per-period locking once exported

---

## Workflow & Acceptance

### State Machine

- **Configurable per issue type**, per project
- Default Work Item workflow oriented around the federal-T&M hand-off pattern:

```
Backlog
  ↓
In Progress
  ↓
Code Review
  ↓
Internal QA
  ↓
Awaiting Client Review   ← clock stops on internal SLA metrics
  ↓
Client Feedback          ← loops back to In Progress if feedback
  ↓
Accepted                 ← the only state that counts as "done"
```

### The Done Boundary

- **"Accepted" is the only state that counts as completion** for milestone scope_pct math
- Internal "done" states (Internal QA, Awaiting Client Review) do not move milestone progress
- Keeps the contractual conversation honest

### Cycle Time, Dual-Tracked

- `internal_cycle_time` = "In Progress" → "Awaiting Client Review"
- `total_cycle_time` = "In Progress" → "Accepted"
- Gap = client-side time

The diplomatic conversation tool when clients complain about pace: *"Average internal cycle is 3 days. Average client review/revision cycle is 11 days."*

### Acceptance Criteria — First-Class Objects

```
AcceptanceCriterion
├─ work_item_id
├─ description (text)
├─ status (pending | met | waived)
├─ checked_by_user_id
├─ checked_at
└─ comment (optional reason — "waived because client agreed in 7/15 meeting")
```

- Per-Work-Item checklist
- Soft enforcement: can't move to "Accepted" until met (admin override possible, logged)
- Multiple levels: project-wide DoD, per-item criteria, milestone-level contractual criteria
- Filterable: "show me all Work Items where any criterion is waived"
- Rolls up: Epic shows aggregate criteria status

### Client Review Affordance

When a Work Item hits "Awaiting Client Review":

- Designated approvers get notification
- Client dashboard shows "Awaiting your review" queue
- Issue page shows: story, criteria as checkboxes, comment box, three buttons:
  - **Approve** → Accepted, criteria auto-checked, client recorded as `checked_by`
  - **Request Changes** → back to In Progress, comment recorded
  - **Discuss** → comment only, no state change
- Three-button affordance is the whole game. Replaces "client tries to find the right column to drag a card to."

### Workflow Configuration

- **Conditions** on transitions: who can transition, what permission needed, what fields must be set
- **Validators** for data integrity (required field check, parent-must-be-in-state check)
- **Post-functions explicitly NOT in core** — that's n8n's job. Line: product enforces *data integrity*, n8n enforces *business policy*.

---

## Custom Fields & Templates

### Custom Fields

- Field types: text, long text, number, date, datetime, single-select, multi-select, user, multi-user, checkbox, URL, currency, formula (computed)
- Workspace-level fields (global) + per-project additions
- **Conditional visibility:** show field X only when `issue_type = 'Bug'` (or other rule). Avoids 40-field bug-filing forms.
- Auto-registered as queryable in the query language

### Issue Templates

- Per-project templates: Bug, Customer-reported, Security incident, etc.
- Pre-fill: required fields, default values, description boilerplate, default assignee, default labels, default child Work Items
- Federal-friendly: every contract has its own intake form structure

---

## Search & Views

### Query Language

YouTrack-style, the universal filter mechanism.

**Syntax:**

```
priority: high assignee: me state: open
project: "Customer Portal" -state: done
created: 2026-01-01 .. 2026-03-31 sort by: -updated
in milestone: "Q2 Launch" priority: critical or priority: high
under epic: PROJ-42
```

**Smart values:** `me`, `today`, `this sprint`, `current milestone`, `unresolved`

**Implementation:**

- **Hand-rolled recursive descent parser** (~500 lines C#) — own the autocomplete logic, give human-friendly errors
- Parse to typed AST → EF Core LINQ expression tree → provider-appropriate SQL
- **Field metadata registry** — each queryable field declares name, aliases, type, valid operators, autocomplete source. One place to add new fields.
- Custom fields auto-register as queryable

### Autocomplete

- Type `as` → suggest `assignee:`, `archived:`
- Type `assignee: ` → suggest project members, `me` pinned at top
- Type `state: ` → suggest the project's actual states (not generic todo/done)
- Type `under: ` → suggest Epics in scope
- Real engineering investment — the language is only good if autocomplete is good

### Persistence

- **Saved queries are first-class objects** with name, query string, owner, scope (personal/project/public), color/icon
- Sidebar treatment like Slack channels
- **Notification rules** = saved queries + delivery method
- **Dashboards = grids of saved queries** rendered as widgets (count, list, chart, board)
- **URL persistence** — every list view's query in the URL, bookmarkable, shareable

### Filter Builder UI

- Visible default for non-power-users
- Compiles to query language
- Switchable to raw query bar for power users

### Matrix Boards

The signature board feature. JIRA has neither a real query language nor matrix boards.

- **Two dimensions:** row field × column field
- Both can be any groupable field (state, priority, assignee, sprint, milestone, type, epic, custom enum/user/relation)
- **Single dimension** = classic kanban (no rows)
- Drag a card → both row and column field update atomically
- Killer use cases: Status × Assignee, Priority × Assignee, Status × Sprint, Epic × Status

**Board = saved query + layout config.** Same persistence model as saved queries.

**Mobile boards:** single dimension (user picks rows OR columns), horizontal swipe between columns, tap-to-move (no drag). Different interaction model for the form factor, not a degraded desktop experience.

### Smart Quick-Add

YouTrack-stolen pattern using the same parser:

```
Login button broken on mobile #bug priority:high assignee:scott sprint:current
```

Parsed → submitted in one shot. Power-user gold. Nearly free once the query parser exists.

### Bulk Operations

- Multi-select checkboxes on list views
- Bulk action bar: change status, assign, add to sprint, label, move, delete
- Wired to API bulk endpoints

---

## Issue Surface

### Single-Page View

- **No tab-hidden content.** Description, fields, comments, history, time, links, dev panel, attachments — all in one scrollable page with sensible structure
- **Inline editing everywhere.** Click field → edit → tab. Modals only for genuinely complex actions (workflow editor, admin config).

### Comments

- TipTap editor — fast, predictable, no JIRA-style keystroke-eating
- @mention autocomplete (users)
- #issue autocomplete (recent + matching by title/key)
- Paste an issue URL → smart link card
- **Internal vs client-visible toggle on every comment** — YouTrack/Zendesk pattern. Critical for client-facing projects.

### Attachments

- Drag-and-drop anywhere on issue page
- Paste-to-attach (Cmd+V from clipboard)
- Inline image rendering in descriptions and comments (not just attached, *displayed*)
- **Image annotation tool** (arrows, boxes, blur for redaction) — small-but-mighty for bug reports
- Preview without download (PDF, images, common docs)
- Storage abstraction: filesystem default, S3/Azure Blob configurable

### History / Audit Trail

- Every field change recorded: who, what, from→to, when
- Per-issue history tab visible to users
- The "client says we never agreed" receipt

### Watchers

- Beyond assignee + reporter, users can watch any issue
- Mass-subscribe via saved query ("notify me about anything tagged 'security'")
- Most-watched issues bubble in some views

### "My Work" Personal Home

- Default landing page: assigned to me, mentioned-and-unread, watching, reported by me
- Customizable, defaultable

---

## Communication

### Built-In Email Notifications

**Default events:**

- @mention in comment or description
- Issue assigned to you
- Issue you reported has state change
- Watched issue updated (configurable: comments only / state changes only / everything)
- Comment on issue you commented on
- Awaiting Client Review hits an issue you approve
- Acceptance criterion met or waived
- Sprint start/complete (members)
- Milestone health threshold crossed (owners)
- Invitation to system/project/workspace
- Email change verification (security-critical, can't disable)
- Password reset (security-critical, can't disable)

### Per-User Preferences

Per event type:
- ✅ Email (default for most)
- ✅ In-app (bell icon)
- 🔌 External (via n8n) — informational flag, exposed in webhook payloads
- ❌ Off

Global toggles:
- Digest mode (hourly or daily, formatted nicely)
- DND hours (timezone-aware)
- "Don't notify me of my own actions" (on by default)
- Per-project mute

### Email Infrastructure

- Real templates (Razor or Liquid), not string concatenation
- Mobile-friendly responsive HTML + plain-text fallback
- Per-workspace branding (logo, color, custom from-name)
- Reply-to threading via In-Reply-To headers (Gmail/Outlook grouping)
- **Reply-by-email-to-comment** — parse incoming, attach as comment, attribute to sender
- SMTP-configurable, pluggable provider abstraction
- Outbound queue with retries, bounce handling, rate limiting
- Warn admins about `noreply@` addresses (spam-flagged, amateurish)

### External Channel IDs

User profile has optional fields:
- `slack_id`
- `teams_id` / `teams_email`
- `discord_id`
- `phone` (for SMS via n8n + Twilio/SNS)
- Generic `external_ids` JSON blob

Webhook payloads include these fields, so n8n flows route to the right channel without lookup overhead.

**The line:** email + in-app in core. Slack/Teams/Discord/SMS = n8n. Hold the line — every "just one more channel" request is rejected with the rationale that n8n does it better.

### Email-In for Issue Creation

- Forward to `project-key@your-tracker.example.com` → creates issue
- Body → description, subject → title, attachments come along
- Sender → reporter (creates guest account if needed)
- Clients without accounts can email bugs in. JIRA Service Management's wedge feature, free here.

---

## Git Integration

### Providers (v1)

1. **GitHub** (Cloud + Enterprise Server)
2. **Bitbucket Cloud** (Bitbucket Server in v1.x)
3. **Azure DevOps Repos**
4. **Gitea + Forgejo** — the differentiator
5. **GitLab** (Cloud + self-managed CE/EE)

### Architecture

- **`IGitProviderAdapter` interface** with: ParseWebhook, VerifySignature, FetchCommit, ListBranches, etc.
- One adapter per provider
- Core consumes only canonical event types
- **Adding a new provider = implementing one interface.** Plugin-friendly.

### Inbound (v1 must-have)

- Webhook receivers, HMAC verification, queued processing, retry/DLQ
- 200-OK-then-process pattern (non-negotiable — Git providers retry on non-200)
- Per-connection delivery logs viewable in admin
- **Issue key detection:** `\b[A-Z][A-Z0-9_]+-\d+\b` with project key validation
- Detected in: commit messages, PR titles/bodies, branch names, comments
- Multiple keys per artifact supported

### Smart Commit Commands

Opt-in per project, default off:

```
git commit -m "Fix login redirect PROJ-42 #fixed"
git commit -m "Update copy PROJ-42 #time 2h"
git commit -m "Address review PROJ-42 #comment looks good now"
```

- `#fixed` / `#resolved` / `#closed` — transition to done state
- `#in-progress` — transition to in progress
- `#time 2h` — log time entry
- `#comment "text"` — add comment from commit
- `#assign @user` — reassign

Audit trail: "issue updated via commit by [author]"

### UI

- Issue page Development panel: linked branches (with state), commits (chronological), PRs (with state badge, reviewers, CI status)
- Epic/Feature rolled-up Development view: every commit, PR, branch across descendants, with filters
- "Create branch" button → v1.x

### The Gitea Differentiator

- First-class implementation, not "GitHub-compatible mode"
- CI integration tests against actual Gitea AND Forgejo containers
- Documentation with screenshots, version compatibility floor explicit
- Marketing line: "first-class Gitea support, tested in CI" — gets you mentioned in r/selfhosted

### Project Keys

- Auto-generated from project name, admin-overridable
- Unique within workspace
- **Immutable after first issue** — renaming breaks every commit reference forever

### Auth (v1)

- Workspace-level service account per provider
- OAuth or PAT
- Per-user OAuth tokens for attributed actions → v1.x

### v1.x and Later

- "Create Branch from Issue" with naming templates and auto-transition coupling
- Per-user OAuth tokens
- Outbound PR creation
- Bidirectional state pushed back to PRs
- Bitbucket Server / Data Center adapter
- AI-powered commit-to-issue auto-linking when keys are missing (v2)

---

## Automation — n8n as the Workflow Engine

### The Architectural Commitment

| Layer | Owns |
|---|---|
| **Product** | Data model, state machine (legal transitions), permissions, UI, API, webhooks, in-product email notifications |
| **n8n (optional bolt-on)** | All automations, integrations, multi-channel notifications, scheduled tasks, business policy enforcement, approval flows |
| **Interface** | Outbound webhooks + inbound HTTP API + first-party n8n node + template library |

**No in-product workflow engine. No DSL. No scripting layer.** Outsource what's already done well.

### Webhook System

Granular events, every meaningful state change emits one:

- `issue.created`, `issue.state_changed` (with from/to), `issue.assigned`, `issue.priority_changed`, `issue.commented`, `issue.field_changed`, `issue.linked`, `issue.acceptance_criterion_met`, `issue.acceptance_criterion_waived`
- `time_entry.logged`
- `sprint.started`, `sprint.completed`
- `milestone.threshold_crossed`, `milestone.health_changed`
- `user.invited`, `user.joined`
- `comment.mentioned`

**Each payload includes everything needed** — full snapshot + diff. n8n shouldn't have to re-query.

**Production-grade delivery:**

- Retry with exponential backoff
- Dead-letter queue for permanently-failed deliveries
- Per-subscription delivery logs (last 50 attempts)
- Signed payloads (HMAC) so n8n can verify authenticity
- Configurable per subscription: events, projects, optional filter expression

### HTTP API

- Whatever the UI can do, the API can do
- REST shape, OpenAPI 3.1 spec published
- Auto-generated TypeScript and Python SDKs
- API tokens scoped per-user and per-integration
- Bulk endpoints for common cases
- Query language available via API (`POST /search`)
- Webhooks list/create/delete via API

### First-Party n8n Node

- Published to npm as `n8n-nodes-yourproduct`
- Resources: Issue, Sprint, Milestone, TimeEntry, User, Project, Workspace
- Operations per resource: Create, Get, Update, Delete, List, Search
- Trigger node: subscribe to webhook events directly
- **Distribution lever** — every n8n user searching for "project management" sees this

### Template Library

5–10 polished starter flows, importable into n8n:

- Bug filed → post to Slack channel
- Monday 9am → email sprint summary to assignees
- Milestone hits 90% budget → email project owner + client
- Issue closed → comment on linked GitHub PR
- Slack message with 🐛 emoji + bot mention → create issue
- New client user joins → welcome email with onboarding links
- Time entry in "do not log" state → alert PM (DCAA hygiene)

Each template = marketing. "Look how easy."

### Admin "Automations" Panel

- Doesn't build a custom automation UI
- Connects to user's n8n instance (URL + API key)
- Shows n8n workflows touching this product, with status, last run, basic stats
- Health check: alerts if n8n unreachable, alerts if users have external prefs set but n8n not configured
- "Browse Templates" links to template library

### License Note

n8n is fair-code (Sustainable Use License), not pure OSS. Self-hosters running n8n alongside this product: fine. Bundling n8n in a future hosted SaaS: requires careful reading. Documented in integration docs.

---

## Reporting

### Built-In Dashboard Widgets

- **Velocity** (points/hours per sprint over time)
- **Burndown** (sprint and milestone variants)
- **Cumulative flow** (WIP by status over time — bottleneck visualization)
- **Cycle time distribution** (histogram, In Progress → Accepted)
- **Time logged by user** (invoice review)
- **Time logged by labor category** (federal billing)
- **Issue volume by reporter** (which client raises the most issues)
- **Aging report** (issues open > N days, grouped by status — triage forcing function)

Built on Recharts. Dashboards = grids of saved queries + chart configs.

### PDF / Printable Views

Issues, sprints, milestones — printable for status meetings.

---

## Operational Hygiene

- **Per-engagement export** (PDF report, JSON archive, CSV time entries) for contract wind-down
- **Workspace archive** (read-only, hidden from default views)
- **Configurable retention policies**
- **Comprehensive audit log** (admin viewable, exportable)
- **SBOM published with each release**
- **Signed releases, signed container images**

---

## OSS Strategy

### License

- **Apache 2.0** for the core
- Patent grant matters; AGPL scares enterprise legal; permissive maximizes trust-and-adoption
- **DCO for contributions, not CLA** — Linux kernel pattern, contributor-friendly
- Plugin interface Apache 2.0; individual plugins can be any license

### Trademark

- Trademark the project name (USPTO filing early)
- Code is open; name and logo are protected
- Forks must use a different name
- "Certified Partner" program for would-be competitors → recurring revenue from competitors

### Monetization Vectors

- **Setup/integration consulting** — natural maintainer-credibility moat
- **Premium plugins** — n8n integration as the wedge, plus BI exports, compliance pack, multi-instance management, SAML, advanced auth
- **Hosted SaaS** — additive, not OSS-replacing. "Bring your own" n8n posture initially.
- **Support contracts** — SLA-backed, the OSS classic
- **Certified Partner program** — partners pay for trademark license + meet quality bar, refer up to maintainer for hard work

### What Stays in OSS Core

Things gating would kill the trust wedge:
- Auth (including OIDC/SSO — gating SSO is the OSS sin)
- Audit log
- 4-tier hierarchy + milestones + sprints
- Time tracking + invoice export
- Basic permissions and roles
- Branding
- HTTP API
- All five Git provider adapters

### What's Legitimate Premium

- n8n integration plugin (your stated wedge)
- Advanced reporting / BI exports (Power BI, Tableau, Looker)
- Compliance pack (DCAA exports, SOC 2 evidence, retention engine)
- Multi-instance management (plane-of-glass for consultancies running 5+ installs)
- Premium auth (SAML, AD direct, SCIM)
- Specific commercial integrations (Jira sync, ADO sync)

### Governance

- BDFL initially, documented in GOVERNANCE.md
- Add maintainers explicitly as project grows
- Lightweight, honest — no fake foundation or steering committee until reality justifies them

### Professional OSS Hygiene (Day-One)

- GitHub repo
- Real CI/CD with visible green checkmarks
- Signed releases (Sigstore or GPG)
- Signed container images (cosign)
- SBOM with each release
- `SECURITY.md`, `security@` address, published vulnerability disclosure
- Semver discipline, real `CHANGELOG.md`
- Pre-built artifacts (Docker images on GHCR, Helm charts in real chart repo)
- Real docs site (Docusaurus / MkDocs Material / Astro Starlight)
- `CONTRIBUTING.md` that actually works (build/test commands accurate)
- `good-first-issue` labels actively curated
- Fast first-response on PRs
- GitHub Discussions for questions

---

## Visual & UX North Star

> *YouTrack's interaction model, shadcn's visual polish, mobile-first responsive instincts, sub-100ms feel.*

### Design Principles

| Principle | Implication |
|---|---|
| **Information density first** | Tight default spacing in data views. Generous spacing for marketing/empty/onboarding only. |
| **Keyboard-first** | Every action has a shortcut, shown in tooltips, surfaced in `?` cheat sheet, command palette covers everything |
| **Inline edit, never modal for routine actions** | Click field → edit. Modals only for genuinely complex actions. |
| **One screen for everything about an issue** | No tabs hiding comments. All in one scrollable view. |
| **Speed is a feature** | Optimistic UI updates, pre-fetch on hover, cache aggressively. Sub-100ms feel. |
| **Query language is a first-class citizen** | Search bar in every list view. Saved queries surfaced prominently. |
| **Editor that doesn't fight users** | TipTap, not hand-rolled contenteditable. Markdown shortcuts, paste-rich-content. |
| **Board view optimized for working** | Dense cards, drag-drop that feels right, keyboard nav, multi-select with shift-click. |
| **Consistency across feature areas** | Workflow config, custom fields, permissions, branding all share the same admin UI shell and patterns. |

### Patterns Stolen Explicitly From YouTrack

- Query language as universal filter mechanism
- Matrix board view (rows × columns)
- Issue commands in comment box (`Assignee: Scott Priority: High`)
- Smart linking
- Anywhere search / command palette (cmd+K)
- Persistent state in URLs

### Patterns Consciously NOT Copied

- YouTrack's dated visual aesthetic — adopt the IA, modernize the surface
- JIRA's modal-everywhere approach
- JIRA's "discover" patterns (features hidden behind menus)
- JIRA's separate workflow editor app
- JIRA's mobile-as-second-class-product
- GitLab's "we do everything badly" platform sprawl

### Mobile-First Reality

Different use cases, different interaction model:

- **Client on mobile:** file a bug with screenshot — 3 taps + photo upload max
- **PM on mobile:** triage queue, reassign, comment — read-heavy with light edits
- **Dev on mobile:** mostly read — checking assigned, reading comments, occasional board card move

Sprint planning, query builder, deep configuration → desktop. Make the 80% read + quick-action flows great on mobile, gracefully tell mobile users "open this on desktop" for heavy admin views.

### PWA from Day One

- Manifest, service worker for shell caching, web push
- "Add to home screen" — sticky behavior JIRA gets, self-hosted competitors don't even attempt
- Cheap now, expensive to retrofit

---

## What's NOT in Scope

### Consciously Skipped

- **FedRAMP boundary** (target buyer's clients run their own approved tools for boundary work)
- **Configurable everything** (JIRA's curse — every setting is forever-tax)
- **Built-in time-off / vacation** (HR system territory)
- **Built-in chat** (Slack/Teams won; Stride died)
- **Built-in CI/CD** (GitLab's mistake)
- **Built-in Git hosting** (don't compete with Git providers; integrate with them)
- **Issue voting / popularity** (2000s-era feature; modern OSS uses GitHub Reactions)
- **Per-issue permissions** (permission-model nightmare; project-level + visibility flags is enough)
- **Native mobile apps** (PWA is the answer)
- **Knowledge base / wiki** (separate product unto itself; competing with BookStack/Outline/Wiki.js is a different project)

### Deferred (v1.x or v2)

- SAML, SCIM
- Roadmap / Gantt timeline view
- SLA tracking
- Customer portal / public anonymous submission
- AI features (auto-summarize, suggest related, auto-categorize) — when shipped, opt-in plugin with configurable LLM endpoint (self-hosted Llama via Ollama for privacy-sensitive shops)
- Time tracking presence detection / passive timer
- Capacity planning calendar
- i18n (architecture in place from day one, translations later, ideally community)
- Themes beyond light/dark

---

## Buyer Profile

> Small DC-area consultancy, 10–50 staff including subcontractors, running 4–8 concurrent T&M engagements with federal-adjacent clients, hosting the tool themselves on their own cloud, needing it to look like a real tool when the COTR logs in, needing it to feed invoicing without being an accounting system, needing to wind down engagements cleanly when contracts end, never paying per-seat for client stakeholder access.

The competition is wrong for this buyer:
- **JIRA** — too heavy, per-seat pricing, doesn't model client engagements
- **YouTrack** — close on functionality, looks dated, per-seat past 10 users
- **Linear / Plane / Vikunja** — too lightweight, no client/T&M concept, no compliance hygiene, looks like a toy
- **Azure DevOps** — federal-compliant but enterprise-rigid, per-seat
- **GitLab** — DevSecOps platform sprawl, doesn't focus on PM
- **OpenProject** — heavy, dated, awkward client UX

This product wins by being JIRA-class on look + YouTrack-class on functionality + Vikunja-class on footprint + uniquely federal-T&M-aware on workflow.

---

## Open Items

These came up but didn't get resolved in the design conversation:

- **Project name** — needed before first commit (URLs, package names, container names get baked early). Search USPTO, GitHub, domain availability. Avoid "-track" / "-board" naming-space collisions.
- **Backup / restore / disaster recovery story** — what's the canonical "client's tracker died, restore it" path?
- **Migrations** — importing from JIRA / ADO / YouTrack / Linear. Real adoption blocker if missing. Probably v1.x with one provider, others as plugins.
- **Plugin architecture details** — model agreed on; actual plugin API surface not yet sketched
- **Observability** — logging, metrics, health checks for self-hosters. What ships in core vs n8n-handled?
- **Compliance posture** — what we *don't* claim (FedRAMP, SOC 2) and what we *do* offer (audit log, SBOM, OSS source review). Worth a docs page being explicit.

---

## Companion Project Status

The n8n-replacement sofa project is **alpha-ready, awaiting real-world use cases.** Worth bearing in mind because:

1. The n8n-replacement was being designed independently. If it matures into something this issue tracker could integrate with, the architectural story changes — "n8n" in this doc could become "n8n OR your-n8n-replacement" with the same interface.
2. Worth keeping the integration surface generic enough that swapping the automation backend is a config change, not a rewrite. Adapter pattern on the n8n integration side, not just the Git side.
3. If the n8n-replacement reaches production-ready before this project ships its automation layer, this project's automation story might be "first-class support for [n8n-replacement], compatible with n8n via shared protocol."

Worth a parameter conversation later.
