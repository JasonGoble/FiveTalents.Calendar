# 0013 — Remove the All Saints' Day Sunday-Alternate Observance

**Status:** Accepted

## Context

ADR 0012 (#42) added `AcnaFeastCatalog.GetAllSaintsSundayObservance`, surfacing All
Saints' Day as an additional `Prescribed` `ObservanceOption` on the Sunday following
Nov 1, per BCP 2019 p.688. Its Consequences section deferred a Canada-specific exception
("when Remembrance Day observances fall on the first Sunday of November, All Saints' Day
may be observed on the preceding Sunday") to #54, which needed a jurisdiction concept the
model didn't have.

While scoping #54, we studied a real reference implementation — liturgical-calendar.com's
ACNA 2019 calendar — directly, to ground what this library's model should cover (see the
"Liturgical Content Parity" milestone this spun off, #56/#58/#59/#60/#61). We specifically
checked November 2027, where Nov 1 falls on a Monday, making Nov 7 a clean, unambiguous
test of the "Sunday following Nov 1" rule. The reference site shows nothing there — just
the ordinary 25th Sunday after Pentecost. It doesn't implement even the U.S. rule ADR 0012
already built, let alone a Canada variant.

This was raised as issue #57: a rubric granted by the BCP but apparently rarely exercised
in practice, weighed against the jurisdiction-concept complexity #54 would have added to
reach full fidelity. #57 posed three options — keep ADR 0012 as-is and drop #54; remove
ADR 0012's rule entirely; or keep ADR 0012 and pursue #54's Canada exception anyway.
Decision: remove the rule entirely. All Saints' Day reverts to a fixed Nov 1 observance
only, matching the reference model and removing a rarely-used code path rather than
extending it further.

## Decision

Remove `AcnaFeastCatalog.GetAllSaintsSundayObservance` and the block in
`AcnaBcp2019Calendar.GetPossibleEucharistObservances` that called it and appended the
additive `ObservanceOption`. All Saints' Day is once again observed only on its fixed
Nov 1 date, resolved the same way as any other fixed Holy Day.

The `"AllSaints"` reading key in `sunday-lectionary.json` (the pre-existing, previously
unreferenced entry ADR 0012 wired up) is left in place rather than deleted — it reverts to
its pre-ADR-0012 unreferenced state. It's legitimately sourced BCP data; removing it would
just mean re-sourcing it if a future issue revives some form of this rubric.

`ObservanceOptionsTests` loses `SundayFollowingAllSaints_AddsAdditionalPrescribedOption`
and `SundayAfterAllSaintsSunday_NoDuplicateObservance`, both of which existed solely to
cover the removed rule.

## Consequences

- Supersedes ADR 0012's rule; ADR 0012 itself is left unedited, per this repo's ADR
  convention (see ADR 0011's handling of ADR 0010 for precedent).
- #54 (the Canada "preceding Sunday" exception) closes as won't-do, referencing this ADR
  and #57 — its underlying rule no longer exists to extend.
- No frontend or API changes: `ObservanceOption`/`ObservancePrecedence` remain unconsumed
  outside `FiveTalents.Calendar` and its tests, same as ADR 0012.
- `ObservanceOption.RubricNote`/`YieldedFeast`'s XML docs are unaffected — the additive
  case they described no longer has a call site, but the fields themselves still serve
  the Sunday-yield and Holy Week/Easter Week suppression cases from ADR 0010/0011.
