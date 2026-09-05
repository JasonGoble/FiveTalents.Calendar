# 0009 — Backlog Organization: Staying on GitHub Issues

**Status:** Accepted

## Context

Two experiments at organizing the backlog into a "sprint-ish" order were tried and retired within the same week (2026-09-04):

1. A GitHub Copilot session added a `.vscode/sprint-board.github-issues` notebook file plus `docs/ai-agent-notes.md`, migrating `CLAUDE.md` to `AGENTS.md` along the way. Reverted (`b9e67e5`, back to `363efe0`) — the sprint-board file is a VS Code-extension-specific artifact, unreadable from any other tool or editor.
2. A `priority:now/next/later` label set, plus a matching single-select field on a new GitHub Project board, was built to answer "what should I work on next." It didn't hold up under its own weight: it duplicated what milestones already express (a feature-set grouping), and it still didn't answer the actual question, because "next" is really a function of *dependencies between issues*, not a static tier assigned once at issue-creation time. Retired the same day.

That failure raised the real question: is GitHub Issues/Projects even the right substrate long-term, or is this reaching for Jira/Azure DevOps too late? The concern (Jason): the backlog is small today only because planning has covered just the base ACNA BCP 2019 implementation — adding other lectionary traditions (RCL is already an `area:*` label) and further feature depth will grow it substantially, and the project is worked on in infrequent, spaced-out sessions where a stable, self-explanatory structure matters more than it would for continuous daily work.

Weighed against that: this is solo-plus-AI-pair work, not a coordinated team, so several things Jira/Azure DevOps are actually built for — sprint velocity/burndown, swimlanes across multiple people, permission schemes, heavy workflow customization — aren't solving a problem this project has. What was actually missing wasn't a tracker feature at all: it was *using* the dependency information already being written into issue bodies (see `## Depends on` sections, e.g. #46/#30) as the real ordering signal, instead of a separate, hand-maintained priority tag that could drift out of sync with it.

GitHub Issues/Projects also has two native features, added since this repo's `AGENTS.md`/Copilot detour, that cover the structural gap without a platform migration:

- **Sub-issues** — parent/child issue hierarchy, i.e. a lightweight epic. Relevant once a new lectionary tradition needs to be tracked as one feature set broken into many issues, the way `milestone` currently groups things but without the extra nesting level.
- **Native issue dependencies** ("blocked by" / "blocks") — a real, queryable relationship, distinct from a prose `## Depends on` paragraph a human (or Claude) has to read and interpret.

## Decision

Stay on GitHub Issues/Projects. Do not adopt Jira or Azure DevOps for this project.

Structural conventions going forward (see also the "Finding the next issue to work" section in `CLAUDE.md`):

- **Milestones** remain the feature-set grouping (e.g. "ACNA BCP 2019 Calendar Engine," a future "RCL Lectionary" milestone).
- **Sub-issues** are used once a milestone's work is naturally a parent feature broken into child issues, rather than a flat list of same-level issues sharing a milestone.
- **Native GitHub issue dependency links** (blocked-by/blocks) are used instead of, or alongside, prose `## Depends on` sections once that relationship needs to be machine-queryable rather than just documented.
- No priority label or field. "What's next" is derived on demand from milestone + dependency links, not pre-assigned and stored.
- The Project board (`FiveTalents.Calendar`, #2) is kept only for its native `Status` field (Todo/In Progress/Done) as a Kanban view of workflow state — not for prioritization or ranking.

## Consequences

- What this gives up relative to Jira/Azure DevOps: a true whole-backlog rankable list (drag-to-reorder across every open issue, not per-milestone), and richer built-in workflow/automation customization. Neither is worth the cost below at current scale.
- What migrating would cost: git-native issue linking (`Fixes #N` auto-close on merge, PR↔issue cross-references), and the fact that the whole backlog is drivable from `gh` in a terminal — Claude Code's `gh` CLI access covers issues, milestones, labels, and Projects v2 today. A second tracker means a second system with its own auth and API to keep in sync with git state, which cuts against the goal of a *stable* structure for someone returning to the project infrequently.
- Reconsider this decision if any of the following actually happens, rather than in anticipation of it:
  - The open backlog grows large enough (rough threshold: 100+ open issues) that milestone + sub-issues + dependency links stop being enough to see the shape of the work at a glance.
  - A second regular contributor (human or otherwise) joins and real multi-person coordination (assignment conflicts, capacity planning) becomes a problem GitHub's primitives don't cover.
  - A concrete need for sprint velocity/burndown tracking shows up — explicitly not a need today per Jason (2026-09-04).
