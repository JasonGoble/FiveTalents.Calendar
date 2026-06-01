# 0002 — Observance Model and Lectionary Data Shape

**Status:** Accepted  
**Supersedes:** [0001 — Liturgical Day Model: Feasts, Commemorations, and Discipline Flags](0001-liturgical-day-model.md)

## Context

ADR 0001 modeled a day's observances as a single nullable `Feast?` (the "winner") plus a `Commemorations` list, with a flat `IReadOnlyList<LectionaryReading> Readings` on `LiturgicalDay`. Implementing the lectionary revealed three problems:

1. **Multiple co-equal feasts are silently discarded.** When a moveable feast (e.g. Trinity Sunday) and a fixed Holy Day (e.g. The Visitation, May 31) share a date, `MaxBy(rank)` kept only one. The other was lost entirely — no observance, no readings.

2. **The season Sunday proper had no named representation.** When a Holy Day fell on a Sunday (e.g. Luke the Evangelist on a Proper 24 Sunday), the Sunday propers were accessible only implicitly through `Season`/`Week` — there was no observance entity a UI could display alongside the feast.

3. **Multiple alternative reading sets per occasion are a first-class BCP concept.** Christmas Day offers three distinct reading sets (I, II, III), each a complete pastoral choice. A flat `IReadOnlyList<LectionaryReading>` cannot represent this without inventing structure that belongs in the data model.

## Decision

### Unified `Observance` list on `LiturgicalDay`

`Feast?`, `Commemorations`, and `Readings` are removed. `LiturgicalDay` gains:

```csharp
public IReadOnlyList<Observance> Observances { get; init; } = [];
```

`Observance` is the public-facing record for a single liturgical observance:

```csharp
public sealed record Observance
{
    public required string Name { get; init; }
    public required FeastRank Rank { get; init; }
    public LiturgicalColor? Color { get; init; }
    public bool IsTransferred { get; init; }
    public IReadOnlyList<ReadingSet> ReadingOptions { get; init; } = [];
}
```

`Observances` is ordered by effective priority descending: non-transferred observances first (sorted by `Rank` descending), transferred observances last. This preserves all co-occurring observances without information loss.

### `ReadingSet` — labeled reading alternatives

Each `Observance` carries `IReadOnlyList<ReadingSet>` rather than a flat reading list:

```csharp
public sealed record ReadingSet
{
    public string? Label { get; init; }  // e.g. "I", "II", "III" for Christmas Day
    public IReadOnlyList<LectionaryReading> Readings { get; init; } = [];
}
```

Most occasions have one unlabeled set. Occasions where the BCP offers alternatives have multiple labeled sets. No set is privileged over another — the label (matching BCP document order) is the only ordering signal.

### Season Sunday proper as a named `Observance`

On Sundays, a season proper observance is always added (e.g. "The Third Sunday of Advent", "Proper 24") using `FeastRank.Minor`. This ensures the season propers are visible in the `Observances` list even on days where a Higher-ranked feast is present — matching the reference presentation on liturgical-calendar.com. Weekday season propers are deferred; they will be handled as part of the Daily Office feature.

### `FeastDay` becomes an internal catalog type

The `FeastDay` record (formerly public, now `internal`) is the catalog entry for a feast or commemoration. It gains two new properties:

- **`IsMoveable: bool`** — true for feasts computed relative to Easter (Palm Sunday, Easter Day, Ascension, etc.). Moveable feasts are never subject to the transfer rule because they define the season rather than interrupting it.
- **`CommemorationCommon?: CommemorationCommon`** — identifies which Common of Saints (`Common_*` JSON key) applies when a commemoration has no specific lectionary readings. All 120+ catalog entries are categorized.

`Observance` is the public-facing type. The conversion from `FeastDay` → `Observance` (with resolved readings) happens inside `AcnaBcp2019Calendar.GetDay()`.

### `CommemorationCommon` enum

Maps to the ten `Common_*` keys in the lectionary JSON:
`Martyr`, `MissionaryEvangelist`, `Pastor`, `TeacherOfFaith`, `MonasticReligious`, `Ecumenist`, `ReformerOfChurch`, `RenewerOfSociety`, `AnyCommemorationI`, `AnyCommemorationII`.

When a feast is not in the specific-readings map, the lectionary falls back to `"Common_" + feast.Common`.

### Lectionary resource naming convention

Lectionary JSON files are embedded resources named `<tradition>-<version>-lectionary.json` (e.g. `acna-bcp2019-lectionary.json`). Each tradition has its own file and its own loader class. The file name is the stable identity — the loader searches by suffix, not by full assembly-qualified name, to survive namespace refactors.

### JSON format

The lectionary JSON supports three entry shapes, distinguished by structure:

| Shape | When used |
|---|---|
| `{ "A": [...], "B": [...], "C": [...] }` | Year-keyed; single reading set per year |
| `[{ type, citation, ... }, ...]` | Flat array; single reading set, year-independent |
| `[{ "label": "I", "readings": [...] }, ...]` | Labeled sets; multiple pastoral alternatives |

## Consequences

- **`day.Feast` and `day.Commemorations` are gone.** Callers use `day.Observances.First()` for the primary observance and filter by `Rank` or `Name` for specific lookups.
- **All co-occurring observances are preserved.** A day with Trinity Sunday and The Visitation will have both in `Observances`, each with their own readings.
- **Transferred feasts are still visible.** `IsTransferred = true` lets a UI communicate "this feast moves to a weekday" without hiding it.
- **Weekday readings in special seasons** (Easter week Mon–Sat, Holy Week Mon–Wed) are surfaced as moveable feast observances in the catalog, not as season propers. This keeps the season-proper path Sunday-only.
- **Daily Office readings are out of scope** for this model. They follow distinct lectionary rules and will be addressed in a separate feature.
