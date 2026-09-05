# 0011 — Holy Week/Easter Week Suppression: Extending `YieldedFeast` to a Second Case

**Status:** Accepted

## Context

ADR 0010 gave `ObservanceOption` a `YieldedFeast` field and an enriched `RubricNote` for one rubric: a non-Principal fixed Holy Day yielding to a Sunday of Advent, Lent, or Easter. Its Consequences section explicitly left #47 — the analogous "what got suppressed" gap for Holy Week and Easter Week (BCP 2019 p.689: "No holy day or observance can replace the fixed propers for Ash Wednesday, Holy Week, or Easter Week") — unaddressed, on the grounds that `AcnaFeastCatalog.GetHolyDays` discards the colliding fixed Holy Day before `AcnaBcp2019Calendar.GetPossibleEucharistObservances` ever sees it, and closing that gap "can't [happen]... without a separate change to `AcnaFeastCatalog`'s API surface."

This ADR makes that anticipated change and closes #47.

## Decision

`AcnaFeastCatalog` gains a second public method alongside `GetHolyDays`:

```csharp
public static FeastDay? GetSuppressedFixedHolyDay(DateOnly date, int year)
```

It reuses `GetHolyDays`' own Holy Week/Easter Week date-range check (extracted into a shared private `IsHolyOrEasterWeek` helper) and the same `_fixedHolyDays` lookup, returning the fixed Holy Day that would have applied on `date` had the suppression rule not discarded it — or `null` if nothing was suppressed. `GetHolyDays` itself is unchanged; this is a read-only, additive query alongside it.

`AcnaBcp2019Calendar.GetPossibleEucharistObservances` calls this when building the single-item `Prescribed` option for a date with no distinct Feast option, and — when it returns non-null and the existing Sunday-yield case (`mandatoryYield`) doesn't already apply — sets:

- `RubricNote`: `"BCP 2019 p.689: {Feast} falls today, but no holy day or observance can replace the fixed propers of Holy Week or Easter Week."`
- `YieldedFeast`: the suppressed `FeastDay`.

The two cases are mutually exclusive by construction: `mandatoryYield` requires a Sunday in Advent, Lent, or Easter, while the Holy Week/Easter Week suppression window is a distinct season (`LiturgicalSeason.HolyWeek`) plus the non-Sunday remainder of Easter Week. A single `if`/`else if` covers both without new branching elsewhere in the method.

`ObservanceOption.YieldedFeast`'s XML doc is updated to describe both cases instead of carving the second one out as future work.

Ash Wednesday is explicitly out of scope, per ADR 0006: no Holy Day is ever suppressed there (a colliding fixed feast is already outranked by Ash Wednesday's own `Principal` rank, so nothing is silently dropped for this method to miss).

## Consequences

- Supersedes ADR 0010's Consequences claim that #47 "is structurally different and unaffected" — it wasn't unaffected, it just needed exactly the `AcnaFeastCatalog` API extension ADR 0010 already anticipated. ADR 0010 itself is left unedited, per this repo's ADR convention.
- `ObservanceOptionsTests.HolyWeekWeekday_YieldedFeastIsNull`'s 2024-03-25 case (Annunciation colliding with Monday of Holy Week) flips from asserting `null` to asserting the suppressed Feast — the exact scenario #47's description used as its worked example.
- No frontend or API changes: `ObservanceOption`/`ObservancePrecedence` remain unconsumed outside `FiveTalents.Calendar` and its tests, same as ADR 0010.
