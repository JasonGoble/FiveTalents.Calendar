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

## GitHub Issues

Every issue created (including ones spun off mid-PR, e.g. "found a gap, opened a follow-up") must get:

- **A type label:** `feature`, `bug`, `chore` (test suites, audits/data-fidelity verification, tooling — anything without a dedicated `feature`/`bug`/`documentation` fit), or `documentation`.
- **`backend` and/or `frontend`** as applicable.
- **An `area:*` label** for what part of the domain it touches: `area:acna`, `area:calendar`, `area:lectionary`, `area:rcl`. Multiple apply when the issue spans areas (e.g. precedence-rule tests touch both `area:calendar` and `area:lectionary`).
- **A milestone**, even for a narrow spin-off — pick whichever open milestone the work actually belongs to (query `gh api repos/JasonGoble/FiveTalents.Calendar/milestones` rather than assuming; don't invent a new milestone without asking first).

`gh label list` shows the full set. Don't invent new labels without asking first.

### Frontend parity

Backend issues have a track record of shipping API/model changes with no companion frontend work (Daily Office readings in #9 landed with the Angular `LiturgicalDay` type never updated — issue #34, milestone "Frontend Parity" caught it after the fact). When closing a backend issue that changes what `GetDay`/`GetRange` returns, check whether the Angular app (`web/five-talents-calendar-web`) needs a companion update. If the frontend work is nontrivial, open an issue in the **Frontend Parity** milestone rather than silently deferring it — don't open speculative frontend issues for backend work that hasn't shipped yet.

### Milestone naming

Milestones are project-management groupings, not release versions — none carry a `vX` prefix. If a real release version is ever needed (e.g. the library's first NuGet publish), it lives in the relevant `.csproj`/`package.json`, not the milestone name.
