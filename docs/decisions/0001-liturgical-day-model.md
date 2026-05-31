# 0001 — Liturgical Day Model: Feasts, Commemorations, and Discipline Flags

**Status:** Accepted

## Context

The BCP 2019 calendar distinguishes several categories of observance that need to be surfaced on a given `LiturgicalDay`:

1. **Principal Feasts and Holy Days** (Red-Letter Days) — take precedence over the season and over each other by rank.
2. **Optional Commemorations** — Anglican and Ecumenical; do not displace the season or a higher-ranked feast; multiple may fall on the same day.
3. **Days of Discipline** (Ember Days, Rogation Days, encouraged fast days) — not feasts at all; the BCP treats them as special observances that overlay any day, including feast days.

## Decision

### Feast vs. Commemorations split

`LiturgicalDay.Feast` carries the highest-ranked Holy Day with rank ≥ `Major` (i.e. Principal Feasts and Holy Days / Red-Letter Days). This is the "primary" observance a congregation would celebrate.

`LiturgicalDay.Commemorations` carries all observances with rank < `Major` (Anglican `Optional` and Ecumenical `Commemoration` ranks). Multiple commemorations can fall on the same day and are returned as a list. They do not displace `Feast`.

### Nullable `FeastDay.Color`

`Color` is nullable on `FeastDay`. Holy Days carry an explicit liturgical color (White, Red, etc.). Commemorations carry `null`, indicating the day takes the surrounding season's color. This avoids assigning a color to commemorations that have no canonical color of their own.

### Days of Discipline as boolean flags

Ember Days, Rogation Days, and encouraged fast days are modeled as boolean properties on `LiturgicalDay` (`IsEmberDay`, `IsRogationDay`, `IsFastDay`) rather than as `FeastDay` entries. Rationale:
- The BCP explicitly separates these from feasts in a distinct "Days of Discipline, Denial, and Special Prayer" section.
- They carry no liturgical color of their own (they inherit the season).
- They can co-occur with a feast (e.g., a Friday Ember Day that is also a Major feast).
- Boolean flags let consumers check each property independently without ranking or precedence logic.

### WeekNumber = 0 for pre-first-Sunday days

Seasons that start on a non-Sunday (Epiphany Jan 6, Ash Wednesday, the days after Pentecost before Trinity Sunday) have a transitional period before their first Sunday. `WeekNumber = 0` on these days is an honest representation of "before Week 1." Display layers render these as "The Nth Day After Epiphany," "After Ash Wednesday," etc.

## Consequences

- API consumers must check both `Feast` and `Commemorations` to see the full picture for a day.
- `WeekNumber = 0` must be handled by any display or API layer; it is not an error.
- Ember and Rogation days are not discoverable via the `Feast`/`Commemorations` collections — callers must check the boolean flags.
