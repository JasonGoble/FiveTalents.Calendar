# 0003 — Sunday Titles: A Property Separate from Feast

**Status:** Accepted

## Context

Several Sundays in the ACNA BCP 2019 calendar carry a special name distinct from their ordinal week designation — "The Baptism of Our Lord" (First Sunday of Epiphany), "Transfiguration Sunday" (Last Sunday of Epiphany), and "Christ the King" (the Last Sunday After Pentecost, always Proper 29). Unlike Trinity Sunday, none of these displace the season or the ordinal week: the First Sunday of Epiphany is still the First Sunday of Epiphany, just with a name layered on top and (for Epiphany's first and last Sundays) its own fixed lectionary propers.

The Last Sunday of Epiphany is the harder of the three to compute: the number of Sundays in the Epiphany season varies year to year (roughly 4–9) depending on when Ash Wednesday falls, so it cannot be identified by a fixed week number.

## Decision

### `SundayTitle` as its own nullable property, not a `FeastDay`

`LiturgicalDay.SundayTitle` (`string?`) carries the special name. It is deliberately **not** modeled as a `FeastDay`:
- `FeastDay` (via `Feast`) represents an observance that can *outrank or replace* the season (see ADR 0001). Baptism of Our Lord, Transfiguration Sunday, and Christ the King never do this — the season and ordinal week stay intact underneath the name.
- Trinity Sunday, which *does* genuinely override the season as a Principal Feast, continues to use `Feast` and does not set `SundayTitle` — the two mechanisms are intentionally kept distinct rather than merged.

### Sunday-gated

`SundayTitle` is only ever non-null when `Date.DayOfWeek == Sunday`. Weekdays within the same liturgical week (e.g. the Monday after the First Sunday of Epiphany) do not inherit the title.

### Last Sunday of Epiphany computed from Easter, not week number

The Last Sunday of Epiphany is always `Easter − 49 days` (Ash Wednesday is `Easter − 46`, and the preceding Sunday is 3 days earlier); the second-to-last is `Easter − 56 days`. Computing these directly from Easter — rather than trying to detect "the last Sunday before Ash Wednesday" via forward week-number counting — sidesteps the season-length variability entirely and is used both for the title and for routing to the correct lectionary propers.

### Fixed lectionary propers for the first and last Sundays of Epiphany take precedence over forward week counting

`sunday-lectionary.json` has always had dedicated `EpiphanyLast` and `EpiphanySecondToLast` entries with the BCP's fixed propers for those two Sundays (e.g. the Transfiguration Gospel), but the reading-resolution code never routed to them — it only matched forward-counted week numbers 1–8, silently returning the wrong Sunday's propers for the last one or two Sundays of Epiphany in every year (not just long ones, since the forward count often still landed within 1–8 by coincidence). Fixed alongside this ADR by checking the Easter-relative dates before falling back to the week-number switch.

## Consequences

- API/UI consumers should treat `SundayTitle` as purely additive display metadata — season, week number, and Proper number are unaffected and remain the source of truth for calendar logic.
- The optional historical names (Septuagesima, Sexagesima, Quinquagesima) are out of scope for this decision — see issue #22. Quinquagesima in particular falls on the same date as Transfiguration Sunday, which raises a field-shape question (single title vs. multiple) not resolved here.
