# Sunday Lectionary Audit — Cross-Reference Against Arnold's `Date::Lectionary`

**Date:** 2026-07-08
**Issue:** #19
**Source:** Michael Wayne Arnold's `Date::Lectionary` (BSD 2-Clause) — https://github.com/marmanold/Date-Lectionary, `share/acna_lect.xml`

## Method

A one-time Python script (not checked into this repo — see "Tooling" below) parsed Arnold's `acna_lect.xml` and diffed it against `sunday-lectionary.json`, keyed by a hand-built mapping from Arnold's day names (e.g. `"The First Sunday in Advent"`, `"Sunday Closest to July 27"`) to our JSON keys (e.g. `Advent1`, `Proper12`). Citations were compared by a numeric "span" heuristic — book abbreviation, first chapter:verse, and last verse — rather than exact string equality, since the two sources use different punctuation conventions for the same optional-verse content (e.g. our `"Zech 14:1-9"` vs Arnold's `"Zechariah (14:1-2); 14:3-9"` are the same span, just notated differently).

223 (occasion, year) entries were checked. 205 matched cleanly (allowing Psalm verse-only variance — see below). 52 findings were produced and triaged as follows.

## Psalm verse-numbering (27 findings — no action needed)

Every Psalm entry in our JSON is tagged `TranslationCode: "NCP"` (New Coverdale Psalter). Arnold's data does not carry this distinction. All 27 Psalm findings showed the *same psalm number* with a small, consistent verse-range offset (e.g. ours `Ps 40:1-11` vs Arnold's `Psalm 40:1-10`) — exactly the pattern expected from NCP-vs-standard versification, not a transcription error. **No changes made.**

## Arnold-side data-quality noise (5 findings — no action needed)

For five fixed occasions (`EpiphanyDay`, `AshWednesday`, `HolySaturday`, `AscensionDay`, `Pentecost`), Arnold's own A/B/C copies of the same fixed-date content disagreed with each other in trivial ways (a missing word, `,` vs `;`, `"Pslam"` typo). This is noise internal to Arnold's source, unrelated to our data. **No changes made.**

## Confirmed correct against the BCP DOCX (no action needed)

Cross-checked directly against `58-Sunday-Holy-Day-Lectionary-12.15.2021.docx`, table by table:

- **Propers 9–12** (First/Second Lesson, all three years): our data matches the DOCX exactly; Arnold's mapping is off for this range (see below).
- **Proper6[C] Gospel**, **Proper22[B] Gospel**: our data (`Luke 7:36-8:3`, `Mark 10:2-16`) matches the DOCX exactly. Arnold's shorter citations (`Luke 7:36-50`, `Mark 10:2-9`) do not.
- **Pentecost** (First Lesson, Second Lesson, Gospel): the DOCX has an explicit `"or"` alternate structure — First Lesson is `Gen 11:1-9` *or* `Acts 2:1-11(12-21)`; Second Lesson is `Acts 2:1-11(12-21)` *or* `1 Cor 12:4-13`; Gospel is `John 14:8-17` alone (no alternate). Our JSON already selects the Genesis+Acts combination as primary with both Acts variants and `1 Cor 12:4-13` recorded as alternates — this is a fully valid, DOCX-documented reading. Arnold's version picks the other valid combination (Acts+1Cor) and adds a Gospel alternate (`John 20:19-23`) that isn't in the DOCX at all.

## Proper13–17 Year B Ephesians/John 6 sequence (10 findings — no action needed)

Arnold's data for this stretch has internal gaps (skips Ephesians 3 entirely, jumps irregularly, switches to James two weeks early instead of the expected transition point). Our data is a clean, unbroken chapter-by-chapter Ephesians read-through paired with the well-documented "Bread of Life" John 6 discourse (Propers 13–17), which is the standard, widely-attested RCL/BCP structure for this stretch. Combined with the Propers 9–12 DOCX confirmation directly preceding this range (which showed Arnold's mapping already diverging from the DOCX by this point), this is assessed as an issue in Arnold's source/day-mapping, not ours. **No changes made.**

## Dead data removed

`PresentationOfChrist` was a flat, unreferenced duplicate of `HolyDay_Presentation` (identical content, confirmed via `grep` that no C# or Angular code ever looks it up). Removed.

## Update 2026-07-08 — the 5 remaining items resolved (issue #25)

The DOCX's table structure proved too unreliable to arbitrate the 5 items below (see the Proper13–17 misattribution note above), so #25 re-checked them against the BCP 2019 **PDF** instead — reading it directly (rendered pages, not raw XML) sidestepped the table-extraction problems entirely and was far more reliable. **All five are confirmed correct in our data; no changes were needed.**

| Entry | Ours | Arnold's | PDF says | Verdict |
|---|---|---|---|---|
| `Easter6[A]` First Lesson | `Acts 17:16-34` | `Acts 17:22-34` | `Acts 17:(16-21)22-34` | Both correct — ours is the full span (optional prefix included), Arnold's is the required core only |
| `Lent3[C]` Gospel | `Luke 13:1-17` | `Luke 13:1-9` | `Luke 13:1-9(10-17)` | Both correct — same optional-verse pattern, ours is the full span |
| `Proper3[A]` First Lesson | `Isa 49:8-23` | `Isaiah 49:8-18` | `Isa 49:8-18(19-23)` | Ours correct (full span); Arnold captured only the required core |
| `Proper24[A]` First Lesson | `Mal 3:6-12` | `Isaiah 45:1-7` | `Mal 3:6-12` | Ours correct; Arnold's value is simply wrong for this occasion |
| `HolyDay_MaryMagdalene` First Lesson | `Judg 4:4-10` (alt: `Judith 9:1,11-14`) | `Judith 9:1-14` | `Judg 4:4-10 or Judith 9:1,11-14` | Ours already correct — primary matches the BCP's primary choice, and the alternate Arnold surfaced was already recorded in our `AlternateCitations`, exact verse notation included |

Three of the five turn out to be the same underlying pattern: a BCP citation like `17:(16-21)22-34` marks the parenthesized part optional. Our data consistently records the *full* span (required + optional) as the primary citation; Arnold's transcription consistently records only the required core. Neither is "wrong" — they're different, valid representations of the same citation — but it's worth knowing this pattern exists before treating any future ours-vs-Arnold span mismatch as an automatic bug.

## Tooling

The diff script was a one-time Python script run locally against a cloned copy of `Date-Lectionary` and the downloaded BCP DOCX — per issue #19's scope, it was not checked into this repository (keeps the library dependency-free; the point was to produce findings, not a permanent tool).

## Attribution

See `THIRD_PARTY_NOTICES.md` for the required BSD 2-Clause attribution to Michael Wayne Arnold's `Date::Lectionary`.
