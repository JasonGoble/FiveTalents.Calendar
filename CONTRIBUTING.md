# Contributing to FiveTalents.Calendar

Thanks for your interest in the project. This is a small, focused library — the process below is meant to keep contributions easy to review, not to gatekeep.

> This project is built through *vibe coding* — conversational, AI-assisted development (see the README). PRs built the same way are entirely welcome; they're held to the same bar as any other: tests pass, formatting is clean, and the change is easy for a human reviewer to follow.

## Before you start

For anything beyond a trivial fix, open an issue first (or comment on an existing one) so we can agree on the approach before you invest time in an implementation. This is especially true for anything touching the ACNA BCP 2019 calendar/lectionary logic — correctness there is verified against the actual Book of Common Prayer text, and getting the source citation right matters more than getting the code merged quickly.

## Development setup

```bash
# .NET library, API, and tests
dotnet restore

# API — http://localhost:5299 (see launchSettings.json)
dotnet run --project src/FiveTalents.Calendar.Api

# Angular frontend — http://localhost:4200
cd web/five-talents-calendar-web
npm ci --legacy-peer-deps
npm start
```

## Before opening a PR

CI runs all of this; running it locally first saves round-trips:

```bash
dotnet format --verify-no-changes   # formatting
dotnet build --no-incremental       # build
dotnet test                         # unit + API integration tests

cd web/five-talents-calendar-web
npm run build                       # frontend build
```

Test coverage has a 70% line-coverage floor enforced in CI (`coverlet`/`reportgenerator`) — new code should come with tests, not just pass existing ones.

## Branching and PRs

- Branch off `main`: `feature/<issue#>-<short-slug>` for features, `fix/<issue#>-<short-slug>` for bug fixes.
- Never push directly to `main` — every change goes through a PR.
- PR descriptions should include a **Summary** (what changed and why) and a **Test plan** (what you ran to confirm it works).
- Keep PRs scoped to one logical change. If you find an unrelated gap while working, open a separate issue for it rather than folding it in.

## Conventions worth knowing before you dig in

A few things that aren't obvious from the code alone (the full list lives in `CLAUDE.md`, which applies to human contributors too, not just AI ones):

- **Enums serialize as strings**, not integers, everywhere in the API (`JsonStringEnumConverter`). Angular `mat-select` values must be string literals to match.
- **Angular state must be a `signal()`.** Plain class properties won't trigger change detection. No `BehaviorSubject`.
- **Every calendar calculation is scoped to a `LiturgicalTradition`.** Don't write tradition-agnostic helpers that quietly assume ACNA rules.
- **Architecture Decision Records** live in `docs/decisions/` — read the index (`docs/decisions/README.md`) before changing how `LiturgicalDay`/lectionary data is modeled, and add a new ADR for any similarly consequential decision. Once accepted, an ADR is never edited — a superseding decision gets its own new ADR.
- **GitHub issues** need a type label, `backend`/`frontend` as applicable, an `area:*` label, and a milestone — `gh label list` shows the full set (see `CLAUDE.md`'s "GitHub Issues" section).

## Reporting bugs / requesting features

Use the issue templates — they ask for what's actually needed to act on a report (for bugs: what you expected vs. what happened, and how to reproduce it; for features: the problem being solved, not just the solution).

## Code of Conduct

This project follows the [Contributor Covenant](CODE_OF_CONDUCT.md). Report unacceptable behavior to the address listed there.

## License

By contributing, you agree your contributions are licensed under the project's [MIT License](LICENSE).
