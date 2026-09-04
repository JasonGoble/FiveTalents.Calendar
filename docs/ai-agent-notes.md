# AI Agent Notes — FiveTalents.Calendar

Durable context for AI coding assistants working in this repo, beyond what's in `CLAUDE.md`.
Migrated from Claude Code memory during the move to GitHub Copilot, 2026-09-03. The architecture
snapshot below is as of PR #48 (2026-07-10) — verify against the current `docs/decisions/` ADR
index and `gh` issues/milestones before relying on any specific PR/issue number here.

## Architecture (as of PR #48 — verify against current ADR index)

`LiturgicalDay`: `Date`, `Season`, `Week` (`LectionaryYear` A/B/C), `Feast?`, `Commemorations`,
`IReadOnlyList<LiturgicalService> Readings` (Sunday/Holy Day Eucharist, often empty on ordinary
weekdays), `required DailyOfficeReadings DailyOffice` (always populated, structurally separate
from `Readings` so they can't collide — ADR 0005), `ProperNumber?`, `IsEmberDay`, `IsRogationDay`,
`IsFastDay`, `SundayTitle?` (Baptism of Our Lord / Transfiguration / Christ the King — ADR 0003).

**`ILiturgicalCalendar.GetPossibleEucharistObservances(date)`** (PR #48, ADR 0008) — the key
current abstraction: returns a precedence-ordered `IReadOnlyList<ObservanceOption>`
(`{ Feast?, Precedence, Services, RubricNote? }`) instead of resolving one answer.
`ObservancePrecedence` is two-tier: `Prescribed` (rubric-sanctioned — sole answer, or one of
several the rubric explicitly grants as equal choices) vs `CommonPractice` (a real deviation
some churches make but the rubric doesn't sanction). Test: **is the collision on a Sunday?**
BCP p.689's discretion clause only covers Sunday collisions. `GetDay`'s `Feast`/`Readings` now
derive from this list's first `Prescribed` item (one source of truth). Eucharist-only —
Daily Office variant blocked on #46 (fixed-Holy-Day Daily Office propers don't exist as data yet).

**Design principle — don't auto-resolve discretionary rubrics:** check a rubric's modal verb
before implementing. "Cannot"/mandatory language → safe to hardcode as unconditional engine
behavior (e.g. no holy day can displace Ash Wed/Holy Week/Easter Week propers — `AcnaFeastCatalog.GetHolyDays`,
ADR 0006). "May"/permissive language → a pastoral/local choice; the engine must surface the
option via `GetPossibleEucharistObservances`, not unilaterally pick one (e.g. yielded Holy Day
transfer target is deliberately left open — ADR 0007, issue #30). Always ask before hardcoding
a resolution to permissive ("may") rubric language rather than picking one silently.

**Every deferred/known-limitation item needs a real GitHub issue**, not just prose in an ADR/PR
description — ADRs are historical records, nothing resurfaces a limitation mentioned only in text.

`DailyOfficeReadings`: `{ MorningPrayer: LiturgicalService; EveningPrayer: LiturgicalService }`.
Regular calendar dates: Year I/II is `date.Year % 2` parity (independent of `Week.LectionaryYear`),
one lesson/office, `ReadingType.MorningPrayer`/`EveningPrayer` (not `FirstLesson`/`SecondLesson` —
~6% of those are actually NT, so no OT/NT implication for Daily Office). Movable Holy Days
(Ash Wed, Maundy Thu–Easter Day, Ascension, Pentecost): exact Easter-offset match, overrides
regular grid, 2 lessons + 1 Psalm, `FirstLesson`/`SecondLesson`. Psalms: 30-day cycle primary,
60-day fallback only on day 31 (day 31 has no 30-day entry — free discretion per BCP, tracked in #30).
Daily Office Psalms always `TranslationCode: "NCP"`.

`LiturgicalService`: `Name?` (null for single-service days; e.g. "Liturgy of the Palms"/"Liturgy
of the Word" for Palm Sunday, always both "Morning Prayer"/"Evening Prayer" for Daily Office),
`Readings: IReadOnlyList<LectionaryReading>`.

`LectionaryReading`: `ReadingType Type` (`FirstLesson`, `Psalm`, `SecondLesson`, `Gospel`,
`MorningPrayer`, `EveningPrayer`), `Citation`, `AlternateCitations` (also covers the Daily
Office's † "optional abbreviation" marker per ADR 0004 — a broadening of its original meaning,
flagged as an unresolved tension in ADR 0004/0005), `TranslationCode?` (`"NCP"` for Psalms, null = default ESV).

`TranslationCatalog`/`TranslationInfo`/`TranslationResourceType` (PR #41, public): resolves
`TranslationCode` → human-readable name from embedded `translations.json`. `AdditionalBooks`
means "on top of the base canon," not "restricted to." Seeded: `ESV`, `ESV-A` (+Apocrypha),
`NCP` (Psalter-only).

**ReadingType is slot-based, not content-based** (ADR 0002) — meaning varies by service:
Sunday Eucharist `FirstLesson` reliably OT/Apocrypha (Acts during Eastertide is the one
exception); Daily Office regular grid `MorningPrayer`/`EveningPrayer` carry no OT/NT implication;
Daily Office movable Holy Days reuse `FirstLesson`/`SecondLesson` positionally, same caveat.

## Data-fidelity sourcing lessons (from #19/#20/#28 audits)
- Michael Wayne Arnold's Perl CPAN `Date::Lectionary`/`Date::Lectionary::Daily` (BSD 2-Clause,
  github.com/marmanold) used as independent cross-reference. `acna-sec_lect_daily.xml` is the
  correct Daily Office file (NOT `acna-xian`, deprecated pre-2019).
- Daily Office "two-year cycle" is NOT 2x the data — one printed table per month (MP+EP pair/day),
  Year I (odd years) uses MP pair, Year II (even) uses EP pair. ~1,460 citations total, not ~2,900.
- NCP Psalm citations have a trailing `v` for versified ranges in the DOCX (e.g. `Ps 78:15-26v`) —
  Arnold's data lacks this distinction; treat verse-range deltas against Arnold as expected, not bugs.
- BCP optional-verse notation `Acts 17:(16-21)22-34` — our transcriptions keep the full span,
  Arnold's keeps only the required core. Both valid; check for this pattern before assuming a bug.
- **PDF is more reliable for manual/hand-verification lookups; DOCX XML (`word/document.xml` via
  stdlib `xml.etree.ElementTree`) is more reliable for bulk byte-exact automated extraction** —
  they flip depending on the task. DOCX floating/anchored paragraphs (e.g. 60-day Psalter's EP
  sidebar column) live outside `<w:tbl>` elements entirely — need full-text `<w:t>` search, not
  table-only extraction.

## Reference: dailyoffice2019.com (github.com/blocher/dailyoffice2019)
Evaluated during #30's design as a reference, not a model to copy. Its settings are almost
entirely "Axis 2" (how a chosen observance is rendered: translation, Psalter cycle, language
register, abbreviated vs full) — it doesn't expose "Axis 1" (which observance governs a date)
to the user at all, unlike this library's actual differentiator. Revisit its settings vocabulary
only if this project grows into real Axis-2 preference territory (translation-as-caller-preference,
Psalter-cycle-as-preference beyond #33's day-31 fallback).

## Process notes specific to this repo
- **Create the branch first**, before writing any code — not after starting work on main.
- Full pre-commit sequence: `dotnet format --verify-no-changes` → `dotnet build` → `dotnet test`
  (if handler/domain/test code changed) → README/ADR doc-sync check → pause for explicit review
  before `git add`.
- Show real running output (curl'd JSON, screenshots) before asking to commit anything with a
  runtime-visible shape — passing tests are necessary but not sufficient; caught a real CSS bug
  and a real API field-naming/semantics bug this way that tests didn't catch.
- `.claude/skills/verify/SKILL.md` in this repo has the full API+Angular+Playwright verify recipe,
  including sandbox gotchas (Playwright needs isolated `npm install`, background dev-server
  cleanup needs `ps aux`/`ss -tlnp` to confirm real PID/port liveness, not just `pkill`/`lsof`).

## BCP 2019 canonical source documents
- Calendar of the Christian Year: `http://bcp2019.anglicanchurch.net/wp-content/uploads/2019/08/57-Calendar-of-the-Christian-Year.{docx,pdf}`
- Sunday, Holy Day & Commemoration Lectionary: `https://bcp2019.anglicanchurch.net/wp-content/uploads/2021/12/58-Sunday-Holy-Day-Lectionary-12.15.2021.{docx,pdf}`
- Daily Office Lectionary: `https://bcp2019.anglicanchurch.net/wp-content/uploads/2021/12/59-Daily-Office-Lectionary-12.15.2021.{docx,pdf}`
- Spot-check computed dates: `https://liturgical-calendar.com/en/ACNA2019/YYYY-MM` (blocks plain WebFetch/403 — needs a browser-like User-Agent if fetched programmatically).
