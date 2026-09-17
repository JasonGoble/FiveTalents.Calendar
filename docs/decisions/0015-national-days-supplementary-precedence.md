# 0015 — National Days: A Third Precedence Tier, Additive Not Competing

**Status:** Accepted

## Context

Issue #56 originally proposed BCP 2019's six civil National Days (Thanksgiving Day —
observed separately in Canada and the United States on different dates but sharing one set
of propers, Canada Day, Independence Day, Remembrance Day, Memorial Day) as bare
informational labels. That premise was wrong, caught mid-implementation via a
liturgical-calendar.com reference snapshot showing Independence Day with a full set of
propers (Collect, Preface, four-lesson reading set) presented *alongside* the day's ordinary
Sunday/season propers — matching `ObservanceOption` (ADR 0008), not `AcnaFeastCatalog
.GetCommemorations` (which carries no Collect/Services/Precedence at all). The issue was
blocked on #59 (ADR 0014), which sourced and staged the five `NationalDay_*` Collect/Preface
keys in `collects-prefaces.json` without attaching them to any date. This ADR is #56's
implementation: computing each National Day's date and wiring the staged content into
`ObservanceOption`.

Three design questions the BCP source itself does not answer were resolved with Jason before
implementation:

### 1. Precedence

The existing `ObservancePrecedence` enum has two values, `Prescribed` and `CommonPractice`,
both describing a Feast *competing* with the day's Sunday/season propers for the same slot.
National Days never compete for a slot — reading `57-Calendar-of-the-Christian-Year.docx`
directly shows them listed between Rogation Days and Commemorations with proper lessons, but
no "takes precedence over" or "yields to" language in either direction. Modeling them as
`Prescribed` would wrongly suggest they can displace the day's own propers (they never
should); modeling them as `CommonPractice` would wrongly suggest they're an unsanctioned
deviation (they're the opposite — the BCP explicitly provides for them).

**Decision:** add a third tier, `ObservancePrecedence.Supplementary` — rubric-sanctioned, has
real content, but never displaces or competes with the day's ordinary propers; always
additive when present. Named `Supplementary` rather than the more obvious `Optional` because
`FeastRank.Optional` already exists as an unrelated concept (a Holy Day rank, not a precedence
tier) — same word, different enum, would be a readability trap for any future reader.

### 2. `GetDay()`'s picker

`GetDay()` previously derived `Feast`/`Readings` via a single equality check:
`observances.FirstOrDefault(o => o.Precedence == ObservancePrecedence.Prescribed)`. Adding a
third tier that this picker never considers would silently make `Supplementary` invisible to
`GetDay()`/`GetRange()` — a real gap given `GetPossibleEucharistObservances` is the
documented single source of truth for precedence (ADR 0008).

**Decision:** generalize the picker to an explicit three-tier ordered fallback:

```csharp
var resolvedOption = observances.FirstOrDefault(o => o.Precedence == ObservancePrecedence.Prescribed)
    ?? observances.FirstOrDefault(o => o.Precedence == ObservancePrecedence.CommonPractice)
    ?? observances.FirstOrDefault(o => o.Precedence == ObservancePrecedence.Supplementary);
```

This is behavior-preserving for every case that predates this ADR: `GetPossibleEucharistObservances`
always constructs at least one `Prescribed` option under current logic (the season/Sunday
propers, or the competing Feast), so the two new fallback terms are unreachable by any
pre-existing case. This is a defensive generalization of the picker's own stated contract, not
an observed behavior change — verified directly against every branch of
`GetPossibleEucharistObservances` before making the change, not assumed.

### 3. Memorial Day's date

BCP text says "the Monday closest to May 28"; the issue's own sourced table renders it as
"last Monday of May." These agree for every year checked 2020–2034 (the only theoretical
disagreement is if May 28 falls on a Friday, in which case "closest" is ambiguous between the
Monday three days before and the Monday five days before, and even that tie is broken toward
the later Monday by the U.S. Uniform Monday Holiday Act's actual scheduling rule).

**Decision:** encode "last Monday of May" — unambiguous, and matches the Uniform Monday
Holiday Act framing actually used to schedule the U.S. federal holiday, rather than the more
literal but ambiguous BCP wording. Documented here rather than silently diverging from the
BCP's literal text.

## Decision

### `ObservancePrecedence.Supplementary`

```csharp
public enum ObservancePrecedence
{
    Prescribed,
    CommonPractice,
    Supplementary,
}
```

### Date computation

Two tradition-agnostic date-math helpers were added to `AcnaBcp2019Calendar`'s private-helpers
region (not `AcnaFeastCatalog`, which owns Feast/Commemoration date tables that National Days
deliberately never touch; not a new dedicated file, matching this repo's existing
bespoke-per-file convention for date math, e.g. `IsRogationDay`):

```csharp
private static DateOnly NthWeekdayOfMonth(int year, int month, DayOfWeek dayOfWeek, int n);
private static DateOnly LastWeekdayOfMonth(int year, int month, DayOfWeek dayOfWeek);
```

Both are pure date arithmetic with no liturgical assumptions of their own — only their call
sites (inside the ACNA-scoped `AcnaBcp2019Calendar`) carry BCP-2019-specific meaning, so this
satisfies CLAUDE.md's tradition-scoping invariant without needing the helpers themselves to be
tradition-scoped.

A private `_nationalDays` table pairs each day's label, `collects-prefaces.json`/
`sunday-lectionary.json` key, and date-resolution function:

| Name | Key | Date rule |
|---|---|---|
| Thanksgiving Day (Canada) | `NationalDay_ThanksgivingDay` | 2nd Monday of October |
| Thanksgiving Day (United States) | `NationalDay_ThanksgivingDay` | 4th Thursday of November |
| Canada Day | `NationalDay_CanadaDay` | July 1 |
| Independence Day | `NationalDay_IndependenceDay` | July 4 |
| Remembrance Day | `NationalDay_RemembranceDay` | November 11 |
| Memorial Day | `NationalDay_MemorialDay` | Last Monday of May |

Remembrance Day and Memorial Day are two independent table entries even though they resolve
to byte-identical Collect/Preface content — matching `collects-prefaces.json`'s own precedent
of storing them as two separate objects rather than one cross-referenced entry. The two
Thanksgivings share one JSON key but get two distinct `Feast.Name` labels, matching the
issue's own "Canada & US (shared content)" framing.

### `GetPossibleEucharistObservances()` — the National Day loop

Appended after all existing option-construction logic, unconditionally on every call:

```csharp
foreach (var (name, key, resolve) in _nationalDays)
{
    if (resolve(date.Year) != date)
    {
        continue;
    }

    options.Add(new ObservanceOption
    {
        Feast = new FeastDay { Name = name, Rank = FeastRank.Commemoration },
        Precedence = ObservancePrecedence.Supplementary,
        Services = AcnaSundayLectionary.BuildServicesForKey(key, info.LectionaryYear),
        Collect = AcnaCollectsAndPrefaces.TryGetCollect(key),
        PrefaceNames = AcnaCollectsAndPrefaces.GetPrefaceNames(key),
    });
}
```

`FeastRank.Commemoration` was chosen because these `FeastDay`s never pass through
`AcnaFeastCatalog.GetHolyDays`'s `candidateFeast` rank comparison (`holyDays.MaxBy(f =>
(int)f.Rank)`) — they're constructed directly inside this loop, so no rank-ordering behavior
depends on the specific value chosen; `Commemoration` is the lowest rank and best matches
their civil, non-liturgical character. No `Color` is set — the BCP assigns none, and guessing
one would be inventing data. The loop runs unconditionally: a National Day can legally
coincide with a Sunday (e.g. 2027-07-04 Independence Day, 2029-07-01 Canada Day), in which
case it simply appends as an extra `Supplementary` option alongside whatever the existing
logic already produced for that Sunday — the additive case this ADR's precedence tier exists
to support.

### Jurisdiction stays unstructured

Per ADR 0012/0013's prior rejection of a structured jurisdiction concept, no `Jurisdiction`
field was added to `ObservanceOption` or `FeastDay` for these six days. Jurisdiction is named
directly in the label text (`"Thanksgiving Day (Canada)"` vs. `"Thanksgiving Day (United
States)"`) — every National Day is always returned by `GetPossibleEucharistObservances`
regardless of jurisdiction, and the consuming application decides what to do with the label,
matching [[feedback_give_data_let_caller_decide_philosophy]]'s "give data, let the caller
decide" principle that has governed every precedence decision since ADR 0008.

### `sunday-lectionary.json`

Five new flat-array keys were added (`NationalDay_ThanksgivingDay`, `_CanadaDay`,
`_IndependenceDay`, `_RemembranceDay`, `_MemorialDay`), matching `HolyDay_Andrew`'s
year-independent shape. No loader changes were needed —
`AcnaSundayLectionary.BuildServicesForKey`/`ParseReadings` already handle arbitrary keys and
the flat-array shape generically.

## Worked examples

| Date (2027) | Day | Result |
|---|---|---|
| 2027-07-01 | Thursday | Single `Supplementary` option: Canada Day |
| 2027-07-04 | **Sunday** | Two options: the ordinary Sunday's `Prescribed` propers, plus a `Supplementary` Independence Day option. `GetDay().Feast` still resolves to the `Prescribed` option — the picker's fallback chain does not change existing resolution. |
| 2027-10-11 | Monday | Single `Supplementary` option: Thanksgiving Day (Canada) |
| 2027-11-25 | Thursday | Single `Supplementary` option: Thanksgiving Day (United States) |
| 2027-11-11 | Thursday | Single `Supplementary` option: Remembrance Day |
| 2027-05-31 | Monday | Single `Supplementary` option: Memorial Day |
| 2027-06-09 | Wednesday | No `Supplementary` option — an ordinary weekday with no National Day |

## Consequences

- `ObservancePrecedence` now has three values; any exhaustive `switch` over it elsewhere in
  the codebase needs a `Supplementary` arm. None existed at the time of this change (verified
  via `grep` — every existing usage was an equality check, not a switch).
- `GetDay()`/`GetRange()` can now, in principle, surface a `Supplementary`-only result on a
  date with no `Prescribed`/`CommonPractice` option at all. No such date exists today (every
  date produces at least a `Prescribed` season option), so this is currently unreachable, but
  is preserved as a defensible outcome should a future date ever have only a `Supplementary`
  observance.
- Frontend Parity: none needed. `ObservanceOption` still is not wired into the API/Angular
  layer at all — `LiturgicalDay.Feast`/`.Readings` are the only derived, flattened view the
  API exposes, and National Days are invisible on those two properties except on the rare date
  they coincide with the `Prescribed` option's own Sunday. Exposing
  `GetPossibleEucharistObservances` through the API remains #61's scope, not this issue's.
