# Daily Office Psalter — Sourced Directly from the BCP 2019 DOCX/PDF

**Date:** 2026-07-08
**Issue:** #28 (Psalter extraction + †/‡ marker design; the Lessons half was #20/PR #29)
**Source:** BCP 2019 Daily Office Lectionary, official ACNA documents (not a third party — no `THIRD_PARTY_NOTICES.md` entry needed):
- PDF: `59-Daily-Office-Lectionary-12.15.2021.pdf` (31 pages)
- DOCX: `59-Daily-Office-Lectionary-12.15.2021.docx`

## Corrected assumptions

Two things #20's audit doc got wrong, corrected by reading the DOCX's own front matter directly (page 736–737 of the PDF):

1. **† is not Easter-dependent.** The front matter states: "The dagger symbol (†) indicates a way to abbreviate a longer chapter if desired." It marks an optional shorter reading, unrelated to any date computation. See ADR 0004 for the full reasoning and the resulting model change.
2. **‡ marks three positions, not four.** The double dagger appears in the calendar-date margin at exactly the earliest possible date each movable Holy Day could fall: Feb 4 (Ash Wednesday), Mar 19 (Maundy Thursday through Easter Day), Apr 30 (Ascension and Pentecost). An earlier, informal count in #7/#20's research had assumed a fourth occurrence that was never located — there isn't one; three matches the three fixed proper-lessons tables printed in the same document (pages 740, 744, 745).

## Method

Two extraction techniques were used and cross-checked against each other:

- **Direct PDF read** (all 31 pages, via page-range reads) — used for the front matter, the 30-day Psalter cycle table, the three movable-Holy-Day tables, and as an independent visual read of several months' 60-day Psalter columns (Jan, Feb, Mar, Apr).
- **DOCX XML parsing** (`word/document.xml`, walked with `xml.etree.ElementTree`, no third-party library) — used for the full 60-day Psalter cycle (all 12 months) and to confirm the movable-Holy-Day tables byte-exact. This was necessary because the 60-day Psalter's Evening Prayer column turned out to be laid out as **floating body paragraphs**, not table cells (the Morning Prayer column *is* a table column; the Evening Prayer column is a separate sequence of anchored paragraphs elsewhere in document order) — a PDF-only read could not reliably distinguish a genuine "v" versification suffix from a text-extraction artifact, since the PDF's rendered text was inconsistent about it (e.g. `78:41-73v` sometimes rendered with a trailing period, sometimes a stray character). Reading the DOCX XML directly confirmed the "v" is a literal, deterministic character in the source (`'18:21-52v'`), not an OCR artifact — it always follows a verse-range citation, never a whole-psalm-number list.

### Cross-validation performed

- **30-day cycle:** the hand-transcribed `acna-bcp2019-daily-office-psalter-30day.json` (from the PDF read) was diffed programmatically against the DOCX's table 0 for all 30 days — **0 mismatches**.
- **60-day cycle:** the DOCX-XML-extracted data for January, February, March, and April was compared against the independent PDF-page read for the same months — **exact match** on every citation, including the anomalous entries (a genuine repeated `80` in August's Evening Prayer column, a genuine `131, 132` insert in March/May's column) that confirmed these weren't transcription slips but real BCP content.
- **Movable Holy Days:** the DOCX table extraction (tables for Ash Wednesday, Maundy Thursday–Easter Day, Ascension–Pentecost) matched the PDF read exactly, citation for citation.
- **Lessons file (†→AlternateCitations) transform:** purely mechanical (regex split on `† <verse-range>` into `citation` + `alternate`), applied to all 182 dagger citations identified in the existing, already-verified `acna-bcp2019-daily-office-lessons.json`. Verified: 0 stray `†` characters remain outside the `_comment` field, all 366 dates preserved, `dotnet test` still passes (278 unit + 13 API tests).

No third-party source (e.g. Arnold's `Date::Lectionary::Daily`, which has no Psalm data at all) was available or needed for this data — everything here is sourced directly from the official BCP text, which is a cleaner provenance chain than the Lessons data.

## Known exceptions and gaps

- **Christmas Day (12-25) Morning Prayer** is the sole exception to the "list of whole Psalms, or one verse-range citation" shape: the BCP names two specific alternative Psalms, `"19 or 45"`, represented as `psalms: ["19"], alternatePsalms: ["45"]`.
- **Day 31 of the 30-day cycle** is deliberately omitted — the BCP grants free discretion ("chosen from among the Songs of Ascents, 120 to 134"), not a fixed assignment. Spun off to issue #30, along with the front matter's separate general "psalms appointed may be reduced in number according to local circumstance" rubric.
- **Feb 29** is included in the 60-day cycle (`02-29`, Morning Prayer Psalm 90, Evening Prayer Psalm 104) sourced from the DOCX's conditional leap-day row, matching the same leap-day row already present in the Lessons data.

## Files produced

- `src/FiveTalents.Calendar/Resources/acna-bcp2019-daily-office-psalter-30day.json`
- `src/FiveTalents.Calendar/Resources/acna-bcp2019-daily-office-psalter-60day.json`
- `src/FiveTalents.Calendar/Resources/acna-bcp2019-daily-office-movable-holy-days.json`
- `src/FiveTalents.Calendar/Resources/acna-bcp2019-daily-office-lessons.json` (revised in place)

All four remain **staging data** — none has an `<EmbeddedResource>` entry yet, and none is consumed by the calendar engine. That, plus reconciling the provisional type labels with the real `ReadingType` enum, is issue #9's job.

## Tooling

The DOCX-parsing script was a one-time Python script (stdlib `xml.etree.ElementTree` only, no third-party dependency) run locally against the downloaded DOCX; per the pattern established in #19/#20, it was not checked into this repository.
