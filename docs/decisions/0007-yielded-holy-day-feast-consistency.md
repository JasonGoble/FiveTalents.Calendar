# 0007 — Yielded Holy Days: Fixing `Feast`, Deferring the Transfer Target

**Status:** Accepted

## Context

ADR 0006 identified two gaps in its Rule 2 ("a Holy Day falling on a Sunday, other than in Advent, Lent, or Easter, may be observed on that Sunday or transferred to the nearest following weekday" — BCP 2019 p.689) and tracked them as issue #43 rather than fixing them inline:

- `LiturgicalDay.Feast` reported the transferred-away Holy Day on the colliding Sunday instead of `null`, even though `AcnaSundayLectionary`'s readings already correctly deferred to the Sunday's own propers.
- The rubric's "transferred to the nearest following weekday" instruction was never actually realized anywhere — the Holy Day simply didn't appear on any date that year.

An initial pass at #43 fixed both by also auto-assigning the yielded Holy Day to the following Monday. On review, that second half over-reached: the rubric says a displaced Holy Day "may" be transferred — permissive, pastoral language, not a directive. Auto-picking Monday (as opposed to Tuesday, or not observing the feast at all — a live option for a lower-rank Holy Day) made a decision on the liturgist's behalf that the BCP explicitly leaves open. This ADR does not revise ADR 0006 (accepted ADRs aren't edited); it records why #43 only partially completes what ADR 0006 identified, and why the rest is being folded into issue #30 instead of decided here.

## Decision

`AcnaBcp2019Calendar.ResolveFeast` now returns `null` for a Sunday of Advent, Lent, or Easter whenever the date's highest-ranked colliding Holy Day is non-Principal — matching what `AcnaSundayLectionary.ResolveKey` already did for `Readings`, so the two are no longer inconsistent with each other.

Nothing surfaces the yielded Holy Day anywhere else. The engine takes no position on whether it's observed on the following weekday, a different weekday, or not at all. This is a deliberate non-decision, not an oversight: representing "here's a rubric-permitted option, pick one" is exactly the class of problem issue #30 (discretionary-rubric representation) already exists to solve — the 30-day Psalter's day-31 substitution was #30's original example; a yielded Holy Day's transfer target is a second, structurally similar one. Building a one-off answer here risked landing on a shape inconsistent with whatever general representation #30 eventually adopts.

## Consequences

- `LiturgicalDay.Feast` is a strictly more conservative signal than before: `null` now means either "nothing was ever scheduled" or "something was scheduled but yielded to the Sunday," and the engine does not distinguish between those two cases or say where the yielded observance went. A consumer that needs to know *which* Holy Day yielded, in order to offer it as a choice, cannot get that from `GetDay` today — that gap is now explicitly #30's to close.
- `PrecedenceRubricTests.HolyDayOnAdventLentOrEasterSunday_YieldsFeastAndReadingsToSunday` pins the `Feast`/`Readings` consistency fix. There is deliberately no test asserting a specific transfer-target date.
- Closes #43 (narrowed to the `Feast`-consistency fix). The transfer-target question moves to #30.
