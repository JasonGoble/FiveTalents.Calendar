# Collects and Prefaces — Sourced from the ACNA BCP 2019

**Date:** 2026-09-06
**Issue:** #59 (model Collects and Prefaces as new content types; ADR 0014)
**Sources:** `resources/acna-bcp-2019-docx/55-Collects-of-the-Christian-Year-12.15.2021.docx`
and `21-Holy-Eucharist-Proper-Prefaces.docx` (both gitignored, local-only, not for
redistribution — direct BCP transcription for this project's own use)

## What was produced

`src/FiveTalents.Calendar/Resources/collects-prefaces.json` — a Preface catalog (six named
entries) plus ten occasion-keyed entries, wired into `AcnaCollectsAndPrefaces` and, at
`AcnaBcp2019Calendar.GetPossibleEucharistObservances`'s four `ObservanceOption` construction
sites, into `ObservanceOption.Collect`/`PrefaceNames`. See ADR 0014.

- **Advent1–4** — real Collects, `["Advent"]` Preface — wired fully end-to-end through
  `GetPossibleEucharistObservances`/`GetDay`.
- **EmberDays** — a staging entry, sourced but not yet attached to any date. Two Collect
  texts (the BCP's "or this" alternate) and `["Apostles and Ordinations"]`. Tested only
  against `AcnaCollectsAndPrefaces` directly.
- **NationalDay_ThanksgivingDay / _CanadaDay / _IndependenceDay / _RemembranceDay /
  _MemorialDay** — staging entries sourced from #56's already-transcribed table, cross-
  checked directly against the DOCX for this audit. Tested only against
  `AcnaCollectsAndPrefaces` directly — nothing computes a `NationalDay_*` key from a date
  yet (#56's job).

## Fidelity spot-checks

| Key | Field | Transcribed value | DOCX value |
|---|---|---|---|
| `Advent1` | Collect (first sentence) | "Almighty God, give us grace to cast away the works of darkness..." | Exact match, "THE FIRST SUNDAY IN ADVENT" |
| `Advent1` | Preface | `Advent` catalog text | Exact match, "ADVENT" heading in `21` |
| `EmberDays` | Collect (primary) | "Almighty God, the giver of all good gifts... all who are [now] called..." | Exact match including the bracketed "[now]", "EMBER DAYS" heading |
| `EmberDays` | Preface | `Apostles and Ordinations` | DOCX `55` cites "Preface of Apostles"; matches `21`'s "APOSTLES and ORDINATIONS" heading text |
| `NationalDay_ThanksgivingDay` | Collect | "Most merciful Father, we humbly thank you..." | Exact match, "THANKSGIVING DAY" heading |
| `NationalDay_CanadaDay` | Preface | `["Trinity Sunday", "Canada Day or Independence Day"]` | See discrepancy note below |
| `NationalDay_RemembranceDay` / `_MemorialDay` | Collect | Identical text for both keys | DOCX `55` gives one combined "MEMORIAL DAY & REMEMBRANCE DAY" entry — duplicated per key here since there's no cross-key reference mechanism (ADR 0014) |

No third-party cross-check source exists for this content (unlike the Arnold-sourced Daily
Office data) — every row above is checked against the BCP DOCX directly, not a second
independent source.

### Discrepancy: Canada Day / Independence Day's Preface

`55-Collects-of-the-Christian-Year`'s own per-day citation for both Canada Day and
Independence Day reads only "Preface of Trinity Sunday" — it does not itself mention a
second option. However, `21-Holy-Eucharist-Proper-Prefaces` has a dedicated "CANADA DAY or
INDEPENDENCE DAY" catalog heading with its own full text, which would have no reason to
exist if it weren't usable for these two days. Issue #56's own transcribed table (written
before this issue existed) lists both as valid options, consistent with treating the
dedicated catalog heading as an available alternate. `collects-prefaces.json` follows #56's
reading (`["Trinity Sunday", "Canada Day or Independence Day"]`) rather than the Collects
document's narrower single citation. Worth a second look if the surrounding rubric text
(not yet located) ever settles this explicitly.

### Note: whitespace normalized

The source DOCX has occasional double spaces before "Amen." (an apparent formatting
artifact, not semantic — unlike the Daily Office lessons' dagger placement, which *is*
semantic and was preserved verbatim in that audit). Collapsed to single spaces here.

### Note: unresolved "Annunciation" annotation on Advent 4

The DOCX places an italicized "Annunciation" label directly beneath "THE FOURTH SUNDAY IN
ADVENT" and above its Collect text, with no further explanation nearby. The Collect text
itself ("Stir up your power, O Lord...") is unambiguous and transcribed as-is; what the
"Annunciation" label is cross-referencing is not resolved by this audit and is not encoded
anywhere in `collects-prefaces.json`. Worth investigating if Annunciation-specific propers
are sourced later.

## Known gaps

- **Full BCP coverage remaining.** Only Advent, Ember Days, and the six National Days are
  sourced. Every other Sunday, Holy Day, and Proper has no Collect/Preface entry yet —
  `AcnaCollectsAndPrefaces.TryGetCollect`/`GetPrefaceNames` return `null`/`[]` for all of
  them, by design (ADR 0014).
- **Ember Days is unwired.** Sourced and tested at the loader level only; no
  `ObservanceOption` attaches it to a date. Ember Days aren't modeled as a feast/season key
  today (`LiturgicalDay.IsEmberDay` is an independently-computed bool).
- **`NationalDay_*` keys are unconsumed** until #56 lands and computes them from a date.
- **Acclamations are deferred** to a follow-up issue not yet filed — no BCP document
  tabulates Acclamation text the way `55` does Collects and `21` does Prefaces; source
  location undetermined.

## Attribution

This is direct transcription from the ACNA Book of Common Prayer 2019 for this project's own
liturgical-calendar use, not third-party redistribution — `THIRD_PARTY_NOTICES.md` doesn't
apply.
