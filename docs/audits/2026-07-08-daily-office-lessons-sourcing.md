# Daily Office Lessons — Sourced from Arnold's `Date::Lectionary::Daily`

**Date:** 2026-07-08
**Issue:** #20 (Lessons-only slice; Psalms and marker design split to a follow-up)
**Source:** Michael Wayne Arnold's `Date::Lectionary::Daily` (BSD 2-Clause) — https://github.com/marmanold/Date-Lectionary-Daily, `share/acna-sec_lect_daily.xml`

## What was produced

`src/FiveTalents.Calendar/Resources/acna-bcp2019-daily-office-lessons.json` — 366 calendar dates (`MM-DD`), each with 4 Lesson citations (Morning First/Second, Evening First/Second), extracted directly from Arnold's XML via a one-time script (not checked in, same pattern as #19).

This is a **staging artifact** for issue #9 to consume, not yet wired into the calendar engine:

- Type labels (`MorningFirstLesson`, etc.) are provisional, not the final `ReadingType` enum — #9 decides the real data model shape (see the Design note in #7 for how the Year I/II runtime rule selects between the Morning and Evening pairs).
- Psalms are not included — see the follow-up issue.
- The embedded resource is **not yet wired into the `.csproj`** (`<EmbeddedResource>`) since nothing consumes it yet; that's #9's job alongside building the loader.

## Fidelity spot-checks

Cross-checked against the BCP 2019 Daily Office Lectionary DOCX (`59-Daily-Office-Lectionary-12.15.2021.docx`), across five months, all exact matches (differences are only the expected book-name abbreviation style — Arnold uses full names, ours will need the same normalization applied during #19):

| Date | Slot | Ours (from Arnold) | DOCX |
|---|---|---|---|
| Jan 1 | Morning First | `Genesis 1` | `Gen 1` |
| Aug 1 | Evening First | `Neh 12 † 27-47` | `Neh 12 † 27-47` (dagger position matches exactly) |
| Mar 15 | Evening First | `Proverbs 14` | `Prov 14` |
| Jun 15 | Morning First | `Joshua 22 † 7-31` | `Josh 22 † 7-31` (dagger position matches exactly) |
| Oct 31 | Morning First | `2 Chronicles 28` | `2 Chron 28` |

## Known gaps (carried into the follow-up issue)

- **Psalms.** Arnold's data has none. Needs real DOCX extraction: the traditional 30-day Psalter cycle table, and the 60-day alternating cycle (found in the `PSALTER–MP` column of each month's table, with the `v` NCP-versification suffix — same convention noted in the #19 audit).
- **Double-dagger (‡) markers.** Arnold's XML preserves the single-dagger (†) marker faithfully (182 occurrences, verified) but has **no double-dagger markers at all** — the DOCX has at least 3 (Feb 3, Mar 18, Apr 29; there were 4 total per an earlier count in #7/#20's research, the 4th wasn't tracked down). These entries will need their citations sourced from the DOCX directly rather than Arnold's XML.
- **†/‡ semantic handling.** The dagger character is preserved verbatim inline within the citation string (e.g. `"Joshua 22 † 7-31"`) exactly as Arnold has it, matching the DOCX's own inline placement. What it actually *means* at runtime (per the BCP's legend — an Easter-dependent substitution) and how it should surface in the data model is deferred to the follow-up issue, not decided here.
- **"Galations" typo.** Both Arnold's data and the source DOCX spell Galatians as "Galations" (missing the second "l") — verified this is not an Arnold-only transcription slip, it's in the official BCP DOCX text itself. Left as-is since it faithfully matches the canonical source; worth a mental note if it ever looks like a bug later.

## Attribution

See `THIRD_PARTY_NOTICES.md` — this is a genuine redistribution of derived data (not just a cross-check like #19), permitted under Arnold's BSD 2-Clause license with attribution retained.
