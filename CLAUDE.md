# FiveTalents.Calendar — Claude Working Guide

Project-specific conventions and technical gotchas. Shared workflow and process policies live in `../CLAUDE.md`.

## Tech Stack (quick reference)

- **Library:** .NET 10 class library — `src/FiveTalents.Calendar` (target: NuGet package)
- **API:** ASP.NET Core 10 minimal API — `src/FiveTalents.Calendar.Api`
- **Tests:** xUnit — `src/FiveTalents.Calendar.Tests`
- **Frontend:** Angular 21, standalone components, Angular Material, signals-first state — `web/five-talents-calendar-web`
- **No ORM / no database** — the library is pure domain logic; no EF Core, no migrations

## Running Locally

```bash
# API
dotnet run --project src/FiveTalents.Calendar.Api   # http://localhost:5299

# Frontend (Angular build path for the pre-commit check)
cd web/five-talents-calendar-web && npm start        # http://localhost:4200
```

## NuGet Packaging

`src/FiveTalents.Calendar` is designed to be published as a standalone NuGet package — consumers can use the library without the hosted API. When the ACNA BCP 2019 tradition is complete:

1. Set `<PackageId>`, `<Version>`, `<Authors>`, and `<Description>` in the `.csproj`
2. `dotnet pack src/FiveTalents.Calendar -c Release`
3. Publish via `dotnet nuget push`

Do not add API or Angular dependencies to the library project — it must remain dependency-free beyond the .NET BCL.

## Key Technical Gotchas

### String enums (critical)
The API uses `JsonStringEnumConverter` globally — all enums serialize as strings, **not** integers. Angular `mat-select` option values must use string literals to match:
```html
<!-- CORRECT -->
<mat-option value="Advent">Advent</mat-option>

<!-- WRONG — will never match API response -->
<mat-option [value]="0">Advent</mat-option>
```

### Angular signals (critical)
All state that drives the template **must** be a `signal()`. Plain class properties are invisible to the scheduler. Use `computed()` for derived state. No `BehaviorSubject`, no manual `markForCheck()`.

### Tradition-scoped calculations
Every method that resolves a liturgical date is scoped to a `LiturgicalTradition`. Do not write tradition-agnostic helpers that silently assume ACNA rules — always pass or inject the tradition and branch explicitly. This is the key invariant that makes multi-tradition support correct.

## Architecture Decision Records

ADRs live in `docs/decisions/`. See `docs/decisions/README.md` for the index. Starting range: 0001+.

## Branching & GitHub Workflow

`main`'s branch protection requires 1 approving review but has `enforce_admins` off, so JasonGoble (repo owner) can push directly — GitHub allows it but prints a "bypassed rule violations" warning every time. That bypass is deliberate for one category of change, not a blanket license:

- **Code changes** — anything touching `src/`, `web/`, or their tests/behavior — go through `feature/<issue#>-<slug>` or `fix/<issue#>-<slug>` → PR → merge, per the existing merge-commit history. Don't push these directly to `main` even though GitHub would technically allow it.
- **Docs/chore-only changes** — `CLAUDE.md`, ADRs, `README.md`, label/Project/milestone config, non-functional tooling — may be committed and pushed straight to `main`. No runtime behavior is at stake, so the review gate isn't buying anything.

This distinction was made explicit 2026-09-04 after a GitHub Copilot session pushed a batch of chore commits directly to `main` (later reverted, `b9e67e5`) without it being a documented convention either way.

## GitHub Issues

Every issue created (including ones spun off mid-PR, e.g. "found a gap, opened a follow-up") must get:

- **A type label:** `feature`, `bug`, `chore` (test suites, audits/data-fidelity verification, tooling — anything without a dedicated `feature`/`bug`/`documentation` fit), or `documentation`.
- **`backend` and/or `frontend`** as applicable.
- **An `area:*` label** for what part of the domain it touches: `area:acna`, `area:calendar`, `area:lectionary`, `area:rcl`. Multiple apply when the issue spans areas (e.g. precedence-rule tests touch both `area:calendar` and `area:lectionary`).
- **A milestone**, even for a narrow spin-off — pick whichever open milestone the work actually belongs to (query `gh api repos/JasonGoble/FiveTalents.Calendar/milestones` rather than assuming; don't invent a new milestone without asking first).

`gh label list` shows the full set. Don't invent new labels without asking first.

### Finding the next issue to work

See ADR 0009 for why this stays on GitHub Issues/Projects rather than Jira/Azure DevOps, and the criteria for revisiting that. There's no priority-tier label or field — with a backlog this size (single digits of open issues), milestone + the "Depends on" notes already written into issue bodies are enough to sequence work, and re-deriving that live avoids a label going stale. To find what's next:

1. Prefer issues in the earliest open milestone (query `gh api repos/JasonGoble/FiveTalents.Calendar/milestones` rather than assuming which is "current").
2. Check the issue body for a `## Depends on` section — if it names another still-open issue, that one goes first.
3. If nothing distinguishes two candidates, it's a judgment call — ask rather than picking arbitrarily.

This replaced two earlier experiments, both reverted/retired: a Copilot-authored `.vscode/sprint-board.github-issues` notebook file (VS Code-extension-specific, reverted in `b9e67e5`), and a `priority:now/next/later` label set plus a matching Project field (retired 2026-09-04 — redundant with milestones and didn't answer "what's next" any better than reading dependencies directly).

The visual board — [FiveTalents.Calendar (Project #2)](https://github.com/users/JasonGoble/projects/2), owned by JasonGoble, linked to this repo — is still there for a Kanban view of workflow state (its default `Status` field: Todo/In Progress/Done). It's not used for prioritization.

### Frontend parity

Backend issues have a track record of shipping API/model changes with no companion frontend work (Daily Office readings in #9 landed with the Angular `LiturgicalDay` type never updated — issue #34, milestone "Frontend Parity" caught it after the fact). When closing a backend issue that changes what `GetDay`/`GetRange` returns, check whether the Angular app (`web/five-talents-calendar-web`) needs a companion update. If the frontend work is nontrivial, open an issue in the **Frontend Parity** milestone rather than silently deferring it — don't open speculative frontend issues for backend work that hasn't shipped yet.

### Milestone naming

Milestones are project-management groupings, not release versions — none carry a `vX` prefix. If a real release version is ever needed (e.g. the library's first NuGet publish), it lives in the relevant `.csproj`/`package.json`, not the milestone name.
