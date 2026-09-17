# 0016 — The Occurrence Stack: One Ordered List Instead of Feast/Commemorations/SundayTitle

**Status:** Accepted

## Context

`AcnaBcp2019Calendar.GetDay()` split "what's happening on this day" three ways, with real
data loss: `LiturgicalDay.Feast` (the single winning option from a three-tier precedence
fallback over ADR 0008's `ObservanceOption` — every *other* ranked option, including a second
`Prescribed` option when a Holy Day collides with an ordinary Sunday, was silently dropped),
`LiturgicalDay.Commemorations` (a disjoint pool with no `Collect`/`Services`/`Precedence` at
all, sourced from a completely different method), and `AcnaBcp2019Calendar
.GetPossibleEucharistObservances()` (the richest shape, but a separate call never folded into
`GetDay()`'s return, and never wired into the API).

Concrete proof, and this ADR's worked example: 2026-12-27 (a Sunday) has John, Apostle and
Evangelist (Major Feast, fixed date) colliding with the First Sunday of Christmas, which is
also the Third Day of Christmas — three simultaneous occurrences. Before this ADR, only John
the Apostle survived into `GetDay()`'s output; the Sunday's own propers were computed
internally but never surfaced with a positive marker, and "Third Day of Christmas" did not
exist as data anywhere in the codebase.

Issue #61 proposed a tier ordering (`Principal > Major > Sunday > SeasonDay >
Anglican/Ecumenical/National > Ember/Rogation/Antiphon`). Checked against the actual code
before building on it (per this repo's standing practice of verifying an issue's premise
rather than trusting it): `FeastRank`'s raw ordinals do **not** correspond to this order —
`FeastRank.Minor` (Rogation Day's rank) was numerically 3, above `Optional`=2
(Anglican commemorations) and `Commemoration`=1 (Ecumenical commemorations), the reverse of
the proposed order. `FeastRank` ranks named-Feast competition; the proposed order ranks a
different axis entirely (occurrence *kind*). A new, purpose-built ordering was required
rather than reusing `FeastRank`.

Four design decisions were made with Jason before implementation:

1. **Replace `Feast`/`Commemorations`/`SundayTitle` outright**, not add a new field
   alongside them. Accepted cost: every C# test site asserting on those three properties
   needed retargeting to the new shape (translating intent, not deleting coverage), plus
   Angular's `day-view`/`week-view`. Accepted because the package carries no `PackageId`/
   `Version` in its `.csproj` — there is no real external NuGet consumer to protect from a
   breaking change to the public shape, and the API has no DTO layer (domain records
   serialize directly), so this is a genuine breaking change to `GET /calendar/*` — but to
   nothing that ships today.
2. **Design the full occurrence-type ordering now**, with Ember Day and Antiphon as empty
   placeholder tiers, rather than waiting for #60 (Ember/Rogation content) or a future
   Antiphon issue to land first. The mechanism exists; the content catches up later.
3. **Give the Sunday its own explicit, positively-identified occurrence type** rather than
   inferring "it's a Sunday" from `Feast == null`. Every Sunday gets a display name now
   (`GetSundayName`, replacing the old `GetSundayTitle`, which only special-cased three named
   Sundays), not just the two BCP-specific titles ("The Baptism of Our Lord",
   "Transfiguration Sunday") plus "Christ the King".
4. **Fix Rogation Day's duplicate date computation as a drive-by.** Before this ADR, Rogation
   Day's three dates (Easter + 36/37/38) were computed independently in two places —
   `AcnaFeastCatalog.GetCommemorations`'s inline loop (which also assigned it
   `FeastRank.Minor`, the enum's only use of that value) and
   `AcnaBcp2019Calendar.IsRogationDay`'s separate Ascension-offset expression — with no shared
   source of truth.

## Decision

### `OccurrenceType` (new enum, `FiveTalents.Calendar.Calendar`)

```csharp
public enum OccurrenceType
{
    PrincipalFeast,
    MajorFeast,
    Sunday,
    SeasonDay,
    AnglicanCommemoration,
    EcumenicalCommemoration,
    NationalDay,
    EmberDay,
    RogationDay,
    Antiphon,
}
```

A new, purpose-built ascending ordinal — not reused from `FeastRank`, for the reason
documented under Context.

### `Occurrence` (new record, subsumes and replaces `ObservanceOption`)

```csharp
public sealed record Occurrence
{
    public required OccurrenceType Type { get; init; }
    public string? Name { get; init; }
    public FeastDay? Feast { get; init; }
    public required ObservancePrecedence Precedence { get; init; }
    public IReadOnlyList<LiturgicalService> Services { get; init; } = [];
    public string? RubricNote { get; init; }
    public FeastDay? YieldedFeast { get; init; }
    public Collect? Collect { get; init; }
    public IReadOnlyList<string> PrefaceNames { get; init; } = [];
}
```

Every field `ObservanceOption` had is preserved unchanged; `Type` and `Name` are the only
additions. `ObservancePrecedence` (`Prescribed`/`CommonPractice`/`Supplementary`, ADR 0008/
0015) is reused with **zero new values** — its applicability widens rather than changes:
`Prescribed`/`CommonPractice` only ever appear within the four competing tiers
(`PrincipalFeast`/`MajorFeast`/`Sunday`/`SeasonDay`); every additive tier
(`AnglicanCommemoration`/`EcumenicalCommemoration`/`NationalDay`/`EmberDay`/`RogationDay`/
`Antiphon`, plus the Christmastide `SeasonDay` naming annotation described below) is always
`Supplementary` by construction, matching ADR 0015's definition exactly.

### `LiturgicalDay` — field disposition

| Field | Disposition | Why |
|---|---|---|
| `Date`, `Season`, `Week`, `DailyOffice`, `ProperNumber` | Unchanged | Orthogonal to the split/loss problem |
| `Feast` | **Removed** | Replaced by `Occurrences` — this was the lossy field |
| `Commemorations` | **Removed** | Replaced by `Occurrences`' `AnglicanCommemoration`/`EcumenicalCommemoration` entries |
| `SundayTitle` | **Removed** | The `Sunday`-type `Occurrence.Name` is a strict superset — every Sunday gets a name now, not just three special-cased ones |
| `Readings` | **Kept, re-derived** | Not itself lossy — every `Occurrence` carries its own `Services`. Re-derived via the same three-tier fallback `GetDay` already used, now applied over `Occurrences` |
| `IsEmberDay`, `IsRogationDay` | **Kept, re-derived** | `Occurrences.Any(o => o.Type == OccurrenceType.EmberDay / RogationDay)` |
| `IsFastDay` | **Unchanged** | Never Feast-shaped, never part of this problem |
| `Occurrences` (`IReadOnlyList<Occurrence>`) | **New** | The unified, ordered stack |

### Sunday naming (`GetSundayName`, replaces `GetSundayTitle`)

Pure string formatting off already-computed `Season`/`WeekNumber`/`ProperNumber`, no new
sourcing. Preserves the two existing BCP-specific special cases verbatim ("The Baptism of Our
Lord" for the First Sunday of Epiphany; "Transfiguration Sunday" for the Last Sunday of
Epiphany, computed as Easter − 49 days) and "Christ the King" for Proper 29. Every other
Sunday in Advent/Christmas/Epiphany/Lent/Easter gets `"The {Ordinal} Sunday of {Season}"`;
every other Sunday in OrdinaryTime gets `"The Sunday after Pentecost (Proper {N})"`.
Sundays that are themselves owned by a Feast (Trinity Sunday, Easter Day, Palm Sunday — where
the Feast's lectionary key equals the season's) never reach this path at all: the single
`PrincipalFeast`/`MajorFeast` entry already *is* that Sunday's identity, matching ADR 0008's
existing rationale, so no separate `Sunday`-type occurrence is ever emitted alongside it.

### Christmastide day naming — deliberately not a general scheme

`SeasonResolver.GetChristmasDayNumber(DateOnly) → int` was extracted from the day-count
formula already computed twice inline in `SeasonResolver.Resolve` (once for the Dec 25–31
range, once for the Jan 1–5 range). `AcnaBcp2019Calendar` uses it to add a `SeasonDay`
occurrence named `"The {Ordinal} Day of Christmas"` on **every** date in Christmastide,
independent of whatever else that date carries — it is computed separately from the Feast/
Sunday competition, which is why it coexists with both in the worked example below rather
than being "the same thing" as either. No other season gets day-of-season naming in this
ADR — Advent/Epiphany/Lent/Easter/OrdinaryTime ferial naming is deliberately out of scope,
tracked as future work, the same pattern as the Ember Day/Antiphon placeholders.

### Rogation Day consolidation

`AcnaFeastCatalog.IsRogationDay(DateOnly, int) → bool` was added, mirroring the file's
existing `IsEmberDay` signature convention, using the Easter + 36/37/38 expression.
`AcnaBcp2019Calendar.IsRogationDay` (the second, independent computation) was deleted. The
inline `Rank = FeastRank.Minor` Rogation Day entry was removed from
`AcnaFeastCatalog.GetCommemorations` — Rogation Day is now built directly as its own
`OccurrenceType.RogationDay` entry, not routed through the generic commemoration list. This
retires `FeastRank.Minor` from the enum entirely (it was that value's only use in the
repository). Accepted as a breaking change to a public enum for the same reason as the rest
of this ADR: no published NuGet consumer exists yet.

### `AcnaBcp2019Calendar` restructuring

Three private builders replace the single inline body of the old
`GetPossibleEucharistObservances`:

- `BuildEucharistOptions(date)` — the four competing tiers (`PrincipalFeast`/`MajorFeast`/
  `Sunday`/`SeasonDay`) plus the always-additive `NationalDay` tier. Same logic as the old
  `GetPossibleEucharistObservances` body, retyped to `Occurrence` and tagged.
- `BuildCommemorations(date, year)` — from `AcnaFeastCatalog.GetCommemorations` (now
  Rogation-free), tagging `FeastRank.Optional → AnglicanCommemoration` and
  `FeastRank.Commemoration → EcumenicalCommemoration`.
- `BuildDisciplineOccurrences(date, year, season)` — `EmberDay`, `RogationDay`, and the
  Christmastide `SeasonDay` naming described above.

`GetPossibleEucharistObservances(date)` now returns `BuildEucharistOptions(date).OrderBy(o =>
o.Type)` — same filter semantics as before, new return type. `GetDay(date)`'s `Occurrences`
concatenates all three builders, ordered by `Type`; `Readings`/`IsEmberDay`/`IsRogationDay`
are re-derived from the combined list.

### API and Angular

**`Program.cs`: zero changes.** `LiturgicalDay` serializes directly (no DTO layer) — the new
`Occurrences` property and `OccurrenceType`/`ObservancePrecedence` enums flow through
automatically via the existing global `JsonStringEnumConverter`. No dedicated new endpoint —
`LiturgicalDay.Occurrences` is now a superset of what a standalone
`GetPossibleEucharistObservances` endpoint would have offered.

**Angular** (`liturgical-day.model.ts`, `day-view`, `week-view`): the removed
`feast`/`commemorations`/`sundayTitle` fields are replaced with `occurrences: Occurrence[]`
and new `Occurrence`/`OccurrenceType`/`Collect` interfaces. `day-view` replaces its separate
feast-card/commemorations-card with a single loop over `d.occurrences`, one expansion panel
per occurrence, badged via a new `occurrence-type-label.pipe.ts`. `week-view` keeps showing
exactly one label per day-cell (`day.occurrences[0]`), matching the deferred richer-rendering
scope tracked on issue #64.

## Worked example — 2026-12-27

```
[0] MajorFeast  "John, Apostle and Evangelist"   Prescribed    (John's readings, no Collect/Preface sourced)
[1] Sunday      "The First Sunday of Christmas"  Prescribed    (Christmas-1 propers)
[2] SeasonDay   "The Third Day of Christmas"     Supplementary (no services yet)
```

`Readings` (the kept field) resolves to `[0].Services` via the same three-tier fallback —
unchanged observable behavior for that field. Verified directly against a running instance of
the API and both Angular views before this ADR was accepted.

## Consequences

- **Breaking change** to the public shape of `LiturgicalDay` (`Feast`/`Commemorations`/
  `SundayTitle` removed) and to `ILiturgicalCalendar.GetPossibleEucharistObservances`'s return
  type (`ObservanceOption` → `Occurrence`). Accepted since the `.csproj` has no `PackageId`/
  `Version` — there is no real external consumer today. This explicitly supersedes ADR 0008's
  framing of `GetDay()`'s `Feast`/`Readings` as "a derived view" left deliberately lossy at the
  time — this ADR is that deferred second act.
- `ObservancePrecedence`'s applicability widens to cover every additive tier, with no new
  enum values.
- `FeastRank.Minor` is removed from the enum.
- Christmastide is the only season with day-of-season naming; Ember Day and Antiphon remain
  content-free placeholder tiers. Both are tracked as future work (#60 for Ember/Rogation
  content; no issue yet for Antiphons or general ferial naming).
- Frontend Parity issue #64 ("expose `ObservanceOption` through API and Angular") is subsumed
  by this ADR's Angular changes — `Occurrences` already carries everything #64 wanted exposed
  (Collects, Prefaces, Precedence, National Days), rendered additively in `day-view`.
