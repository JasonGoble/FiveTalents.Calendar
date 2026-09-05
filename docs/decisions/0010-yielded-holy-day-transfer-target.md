# 0010 — Yielded Holy Day Transfer Target: Structured Data, No Cross-Date Lookback

**Status:** Accepted

## Context

ADR 0008 closed the Eucharist half of #30 but explicitly left one piece open: "the 'where does a yielded Holy Day actually get observed' question (#30, #43's original over-reach) remains genuinely open... `GetPossibleEucharistObservances` only reasons about the date it's given; it does not look forward or backward across dates to find a transfer target."

That question is BCP 2019 p.689: a non-Principal fixed Holy Day colliding with a Sunday of Advent, Lent, or Easter yields entirely to that Sunday's propers, and "may be... transferred to the nearest following weekday." `AcnaBcp2019Calendar.GetPossibleEucharistObservances` already detects this (`mandatoryYield`) and attaches a `RubricNote` string to the Sunday's sole `Prescribed` option — but that string doesn't mention the transfer possibility, and the yielded `FeastDay` itself (already computed as `candidateFeast` while building the option) is discarded rather than exposed.

#43's first attempt at "realizing" this rubric auto-assigned the yielded Holy Day to the following Monday as `GetDay`'s answer. That was walked back (ADR 0007) because the rubric's "may" is pastoral discretion — a different weekday, or not observing it at all, are both live options the BCP leaves to the officiant. Any design here has to keep that same boundary: expose enough for a consumer to act, without the engine deciding whether or where it happens.

## Decision

`ObservanceOption` gains a new nullable field:

```csharp
public FeastDay? YieldedFeast { get; init; }
```

Populated only on the Sunday's option when `mandatoryYield` is true (the exact `candidateFeast` already computed there); null everywhere else. `RubricNote`'s text is extended to mention the transfer possibility:

> "BCP 2019 p.689: {Feast} falls today, but Holy Days do not displace the propers of a Sunday in Advent, Lent, or Easter; it may instead be transferred to the nearest following weekday."

This is deliberately additive-only:

- No cross-date computation is added. `GetPossibleEucharistObservances` stays scoped to the single date it's given, per ADR 0008's existing boundary.
- The actual transfer date ("nearest following weekday") is trivially `Sunday + 1 day` for any consumer that wants it — the engine doesn't need to compute or assert it.
- Whether the yielded Feast is actually observed on that weekday remains entirely outside the engine's job, same as #43/ADR 0007 established.

### Alternative considered and deferred: surface it as an option on the transfer date itself

A heavier alternative was considered: when `GetPossibleEucharistObservances` is queried for the weekday immediately following such a Sunday, look backward one day and add a `CommonPractice`-tier option there for the yielded Feast (reusing the existing `Prescribed`/`CommonPractice` pattern already used for Red-Letter-Day-skipping). `GetDay`'s single answer would be unaffected (`CommonPractice` never wins `FirstOrDefault(Prescribed)`).

Deferred, not rejected — it crosses the "does not look forward or backward across dates" line ADR 0008 drew, and would need to handle cascading collisions if that weekday is itself blocked (e.g. it's a fixed Holy Day, or falls in Holy Week). `YieldedFeast` alone already answers the practical need (a consumer can identify the yielded Feast and derive the candidate date itself); this stays a possible future issue if a real use case shows up.

## Consequences

- `ObservanceOption`/`ObservancePrecedence` remain unconsumed outside `FiveTalents.Calendar` and its unit tests (confirmed via search of `src/FiveTalents.Calendar.Api` and `web/five-talents-calendar-web`) — this is a pure library-and-tests change with nothing else to keep in sync.
- Closes #30's remaining scope. #47 (the analogous Holy Week/Easter Week "what got suppressed" gap) is structurally different and unaffected: `AcnaFeastCatalog.GetHolyDays` discards that Feast before `GetPossibleEucharistObservances` ever sees it, so `YieldedFeast` can't be populated there without a separate change to `AcnaFeastCatalog`'s API surface.
