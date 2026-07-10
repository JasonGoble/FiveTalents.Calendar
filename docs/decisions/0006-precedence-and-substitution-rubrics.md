# 0006 — Precedence and Substitution Rubrics

**Status:** Accepted

## Context

ADR 0001 establishes that `LiturgicalDay.Feast` carries "the highest-ranked Holy Day... over each other by rank." That's the general rule, but the BCP 2019 Calendar of the Christian Year (doc #57, pp. 688–689) states several more specific rubrics governing precedence and substitution that rank comparison alone doesn't fully capture:

1. Principal Feasts take precedence over any other day or observance.
2. A Holy Day falling on a Sunday, other than in Advent, Lent, or Easter, may be observed on that Sunday or transferred to the nearest following weekday.
3. No holy day or observance can replace the fixed propers for Ash Wednesday, Holy Week, or Easter Week.

Issue #21 inventoried these against the engine's actual behavior. Rule 3 exposed a real bug: Monday–Wednesday of Holy Week are ranked `FeastRank.Major` in `AcnaFeastCatalog`, the same rank as most fixed Holy Days, so a colliding fixed feast (e.g. the Annunciation, March 25, on the Monday of Holy Week in 2024) won the rank tie by list order and incorrectly displaced the fixed Holy Week propers. Easter Week's weekdays have no catalog entry at all, so a colliding fixed feast won uncontested there too.

## Decision

### Rank comparison remains the primary mechanism (Rule 1)

`AcnaBcp2019Calendar.GetDay` continues to pick `holyDays.MaxBy(f => (int)f.Rank)` as `Feast`. This is sufficient whenever the colliding observances have different ranks — Principal always beats Major, which is Rule 1.

### Holy Week and Easter Week are a hard exception (Rule 3), not a rank contest

`AcnaFeastCatalog.GetHolyDays` suppresses fixed Holy Days outright for any date from Palm Sunday through the Saturday after Easter Day (inclusive) — the whole Holy Week + Easter Week range — regardless of rank. This makes Rule 3 unconditional rather than dependent on the rank table never producing a tie. Moveable Holy Week/Easter Day feasts (Palm Sunday, Maundy Thursday, Good Friday, Holy Saturday, Easter Day — all `Principal`) are unaffected, since they were never competing with a fixed feast for the `MaxBy` in the first place.

Ash Wednesday is a single day, not a range, and every fixed Holy Day that could ever coincide with it is out-ranked by its `Principal` rank already — no suppression needed there.

### Rule 2 (Sunday transfer) is only partially implemented — tracked separately

The reading-selection half of Rule 2 is implemented in `AcnaSundayLectionary.ResolveKey`'s `feastTransferred` check: a Holy Day colliding with a Sunday of Advent, Lent, or Easter correctly falls back to that Sunday's own propers. Two gaps remain, deliberately left out of this ADR's scope and tracked as separate issues rather than folded into the #21 test-authoring PR:

- `LiturgicalDay.Feast` still reports the transferred-away Holy Day on that Sunday instead of `null` — issue #43.
- The rubric's "transferred to the nearest following weekday" instruction is never actually realized; the Holy Day simply doesn't appear anywhere that year — also issue #43.
- All Saints' Day's additional Sunday observance (p.688) is a distinct, unimplemented optional rubric — issue #42.

## Consequences

- `AcnaFeastCatalog.GetHolyDays` is no longer a pure "look up what's on this date" function — it has a date-range-based suppression rule baked in, motivated by a rubric rather than by any structural property of the data. Future contributors adding fixed Holy Days should be aware a Holy Week/Easter Week collision is silently dropped, not merged or ranked.
- `PrecedenceRubricTests` (in `tests/FiveTalents.Calendar.Tests.Unit/Calendar/`) pins each rubric above to an explicit, documented test, including the still-incomplete Rule 2 behavior — those tests should be revisited when #43 lands.
