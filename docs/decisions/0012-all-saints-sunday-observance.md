# 0012 — All Saints' Day: Additive Observance on the Sunday Following November 1

**Status:** Accepted

## Context

BCP 2019 p.688: "All Saints' Day may also be observed on the Sunday following November 1,
in addition to its observance on the fixed date." `AcnaFeastCatalog` only ever recognized
All Saints' Day as the fixed Nov 1 entry; nothing surfaced the alternate Sunday observance,
per #42.

While implementing, `sunday-lectionary.json` turned out to already have an `"AllSaints"`
reading key (byte-for-byte identical to `"HolyDay_AllSaints"`, the one used for Nov 1 itself)
sitting unreferenced between the `Proper25` and `Proper26` entries — exactly where the
Sunday-cycle propers for early November belong. This was sourced in an earlier data pass and
never wired up; this ADR's change wires it up rather than adding new lectionary data.

## Decision

`AcnaFeastCatalog` gains `GetAllSaintsSundayObservance(date, year)`, returning All Saints' Day
when `date` is the Sunday strictly following Nov 1 of `year`, or `null` otherwise — including
when Nov 1 itself falls on a Sunday, since that case is already fully handled by the existing
ordinary-Sunday-collision rule (ADR 0008/0006): both the Feast and the season's own Sunday
propers already become their own `Prescribed` options there.

`AcnaBcp2019Calendar.GetPossibleEucharistObservances` calls this independently of the
existing `candidateFeast`/`GetHolyDays` logic (no fixed or moveable Holy Day falls between
Nov 2–8, so the two paths never interact) and, when non-null, appends an additional
`Prescribed` `ObservanceOption` built from the `"AllSaints"` JSON key, alongside whatever
option the season's own Sunday propers already produced.

Unlike ADR 0010/0011's suppression cases, this is purely additive — the rubric explicitly
grants the Sunday observance *in addition to* Nov 1, not instead of the season's Sunday
propers. So `YieldedFeast` is never set here; nothing is excluded. `RubricNote` alone
explains the option's presence — a case that field's doc previously didn't describe (it only
covered absence/constraint), so `ObservanceOption.RubricNote`'s XML doc is broadened to also
cover "additionally offered."

`GetDay`'s `FirstOrDefault(Prescribed)` is unaffected: the season's own Sunday option is
still added first, the All Saints option second, so the fixed Nov 1 date remains `GetDay`'s
answer for the following Sunday's `Feast`/`Readings` — All Saints becomes a second, explicit
alternative in the full `GetPossibleEucharistObservances` list, not a change to `GetDay`.

**Scope:** U.S. case only, per the issue's own instruction. BCP 2019 p.688 also grants a
Canada-specific exception ("when Remembrance Day observances fall on the first Sunday of
November, All Saints' Day may be observed on the preceding Sunday"), which needs a
jurisdiction concept the model doesn't have yet. Deferred to #54.

## Consequences

- `AcnaFeastCatalog`'s public surface grows by one query method, following the same
  read-only, additive pattern as `GetSuppressedFixedHolyDay` (ADR 0011) — no change to any
  existing method's behavior.
- `ObservanceOption.RubricNote` now has three usages (Sunday-yield, Holy Week/Easter Week
  suppression, and this additive case) instead of two; its XML doc reflects all three.
- No frontend or API changes: `ObservanceOption`/`ObservancePrecedence` remain unconsumed
  outside `FiveTalents.Calendar` and its tests, same as ADR 0010/0011.
- The Canada Remembrance Day exception is explicitly out of scope here and tracked
  separately in #54 rather than blocking #42.
