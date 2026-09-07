# 0014 — Collects and Prefaces: A New Content Type, Sourced Partially

**Status:** Accepted

## Context

`ObservanceOption` (ADR 0008) models precedence and Scripture readings for a Eucharistic
observance, but not the prayer texts an officiant actually needs: the Collect and the
Preface. Issue #59 asked for a design pass settling how this attaches, plus a first sourced
content set proving the shape. Issue #56 (the six BCP National Days) is blocked on this —
it already has a fully-transcribed table of Collect/Preface/reading data for those six days
and cannot proceed until this content type exists.

Four design forks were resolved with Jason before implementation, backed by direct
verification against the codebase:

- **Attachment point.** `ObservanceOption`, not `FeastDay` or a new season/week entity.
  `ObservanceOption` is already returned for every date — feast or ferial — via
  `GetPossibleEucharistObservances`, so it covers ferial weekdays with no new modeling, and
  it's the same record `GetDay`'s `Feast`/`Readings` already derive from.
- **Storage.** A separate embedded JSON resource (`collects-prefaces.json`), keyed by the
  same occasion-key vocabulary `AcnaSundayLectionary` already uses for readings
  (`HolyDay_Andrew`, `Advent1`, `Proper16`, etc.) — not merged into `sunday-lectionary.json`,
  since it's sourced from different BCP documents (`55-Collects-of-the-Christian-Year`,
  `21-Holy-Eucharist-Proper-Prefaces`) with independent, partial coverage.
- **Prefaces: a named catalog referenced by key, not inline full text.** Prefaces repeat
  verbatim across many dates by season or occasion name (e.g. every Advent Sunday cites
  "Preface of Advent" verbatim). Direct precedent: `LectionaryReading.TranslationCode` is
  already a string key into a separate `TranslationCatalog`/`translations.json` — the same
  shape, applied to a different repeating value.
- **Collects: full text per entry, with simple alternates.** Collects are largely unique per
  observance (unlike Prefaces), so this reuses `LectionaryReading.AlternateCitations`'s exact
  shape (`IReadOnlyList<string>`, default `[]`) rather than adding catalog indirection.
- **Acclamations: deferred to a follow-up issue.** No BCP document tabulates Acclamation text
  the way `55-Collects-of-the-Christian-Year` does Collects and `21-Holy-Eucharist-Proper-Prefaces`
  does Prefaces — it appears to live inline in the Eucharist rite texts, source location
  undetermined. Nothing is designed or stubbed for it here.

No Frontend Parity companion issue is needed: `LiturgicalDay` (and therefore the API's
`/calendar/{tradition}/day/{date}` and `/range` endpoints) has never exposed `ObservanceOption`,
`Precedence`, or `RubricNote` at all — only the flattened `Feast`/`Readings` derived view.
Adding `Collect`/`PrefaceNames` to `ObservanceOption` changes zero bytes of any API response.
ADR 0008's entire ranked-options model has never been wired into the API/Angular layer; that
gap belongs to whichever future issue (plausibly #56) first exposes
`GetPossibleEucharistObservances` through the API.

## Decision

### `Collect` and `ObservanceOption`

```csharp
public sealed record Collect
{
    public required string Text { get; init; }
    public IReadOnlyList<string> AlternateTexts { get; init; } = [];
}

// added to ObservanceOption
public Collect? Collect { get; init; }
public IReadOnlyList<string> PrefaceNames { get; init; } = [];
```

`Collect` lives in a new `FiveTalents.Calendar.Liturgy` namespace, alongside the internal
`AcnaCollectsAndPrefaces` loader that mirrors `AcnaSundayLectionary`'s load-once-from-
embedded-JSON pattern. `AcnaCollectsAndPrefaces` is kept internal (unlike the public
`TranslationCatalog`, which backs a real `/translations` endpoint) — nothing in #59's
acceptance criteria requires exposing this externally yet.

`AcnaBcp2019Calendar.GetPossibleEucharistObservances` resolves `Collect`/`PrefaceNames` at
each `ObservanceOption` construction site from the *same* key already computed there for
`Services` — the Feast's own key when the option is a distinct Feast option, otherwise the
season/Proper key that produced `Services` for that option. No new key computation.

### Partial, deliberate coverage

`collects-prefaces.json` sources only what #59 needs to prove the shape and unblock #56:
the four Sundays of Advent, the Ember Days ordination Collect (with its BCP "or this"
alternate), and the six BCP National Days. A key with no entry resolves to `Collect: null`,
`PrefaceNames: []` — not an error, not a placeholder. Full BCP coverage is future work,
exactly like `sunday-lectionary.json`'s own history of incremental sourcing.

Ember Days are sourced but **not** wired into `GetPossibleEucharistObservances` as their own
`ObservanceOption` — Ember Days aren't modeled as a feast/season key today (`LiturgicalDay
.IsEmberDay` is an independently-computed bool, not a key `AcnaSundayLectionary` or this new
loader resolves from a date). Attaching an Ember Days `ObservanceOption` is follow-on work.

The six National Days are sourced and tested directly against `AcnaCollectsAndPrefaces`, but
not yet reachable through `GetPossibleEucharistObservances` — nothing computes a
`NationalDay_*` key from a date yet. That's #56's job; this issue only builds the content
type #56 depends on.

### Correction to the original plan: Ember Days does have a Preface

The plan written for #59 assumed Ember Days would source with no Preface, since no
`EMBER DAYS` heading exists in `21-Holy-Eucharist-Proper-Prefaces`. Reading
`55-Collects-of-the-Christian-Year` directly (source of truth for the sourcing step, not the
plan's a-priori assumption) shows both Ember Days Collects cite "Preface of Apostles" —
which matches `21`'s "APOSTLES and ORDINATIONS" catalog heading. `collects-prefaces.json`
sources this accurately (`"prefaces": ["Apostles and Ordinations"]`) rather than following
the plan's unverified assumption. Flagged here per this repo's premise-verification
convention, not silently corrected.

## Worked examples

| Case | Collect | PrefaceNames |
|---|---|---|
| Advent Sunday (e.g. Advent 1) | Its own proper text | `["Advent"]` |
| Canada Day / Independence Day | Its own proper text | `["Trinity Sunday", "Canada Day or Independence Day"]` — the BCP grants a choice of two |
| Ember Days (loader only, not yet an `ObservanceOption`) | Primary text + one `AlternateTexts` entry | `["Apostles and Ordinations"]` |
| An unsourced key (e.g. an ordinary Proper) | `null` | `[]` — deliberate partial coverage, not an error |

## Consequences

- Partial coverage is by design, matching how `sunday-lectionary.json` itself was built up
  incrementally. A caller must treat `Collect: null` / `PrefaceNames: []` as "not yet
  sourced," not "the BCP has none."
- `Collect`/`PrefaceNames` are not exposed through the API yet — consistent with the rest of
  `ObservanceOption`, which the API/Angular layer has never surfaced (see Context).
- Attaching an Ember Days `ObservanceOption` to a date, and computing `NationalDay_*` keys
  from a date (#56), are both follow-on work this ADR does not attempt.
- Acclamations remain unmodeled; a follow-up issue is needed once their BCP source location
  is identified.
