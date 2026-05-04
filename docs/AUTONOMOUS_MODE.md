# Autonomous mode

## When invoked

Phrases like "see how far you can get," "keep going till morning," "do those items autonomously," or any explicit handover for an extended period.

## Defaults

1. Execute via CLI / file edits without confirmation. Pause only when an action requires UI interaction.
2. Commit per logical phase, not per file. Each commit is one feature / refactor / scope.
3. Push after each commit so the work is recoverable from another machine.
4. Use Conventional Commits with the standard `Co-Authored-By` trailer.
5. Apply the defaults table below without confirmation.

## Pause for

- Force pushes to `main`
- `git config` changes (per-repo or global)
- Any action with cross-process or external blast radius (sending mail, posting to Slack, deploying, etc.)
- Decisions that meaningfully change the locked decisions list in `DECISIONS.md`

## Phase template

For each phase:

1. State the phase intent (one sentence).
2. Domain → Application → Infrastructure → Api in that order.
3. Migrations across all three providers when the model changed.
4. Multi-provider integration tests (boundary-pushing).
5. HTTP integration tests when the phase added an endpoint.
6. Run all tests; fix regressions before commit.
7. Commit + push.

## Defaults table

| Question | Default without asking |
|---|---|
| New entity that's an internal counter? | `[NotAudited]` |
| New FK with cascade-path conflict on SQL Server? | `OnDelete(NoAction)` + app-managed cleanup |
| New endpoint validator? | Manual, return `IDictionary<string, string[]>?` |
| New endpoint? | Camel-case JSON, enums as strings, `ProblemDetails` for errors |
| New migration? | Generate for all three providers in one phase |
| New use case? | Multi-provider integration tests via `DatabaseTestBase<TFixture>` |
| New event-emitting handler? | `OutboxEmitter.Emit(...)` before `SaveChangesAsync` |
| New PATCH endpoint? | Null/missing = no change |
| Cross-machine commit? | Push after each phase |

## Stuck?

Document the blocker, move to the next phase. Don't burn time spinning.

## End-of-session

Append to `SESSION_LOG.md` — one row per commit summarising what shipped. Existing entries are the format template.
