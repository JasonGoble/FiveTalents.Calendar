# 0005 — Daily Office Readings Model: Year I/II Division, Reading Types, and the Movable Holy Day Override

**Status:** Accepted

## Context

Issue #28 staged four JSON resources (the calendar-date Lessons grid, the 30-day and 60-day Psalter cycles, and the 7 movable-Holy-Day proper-lessons entries) but explicitly deferred every runtime decision to #9: reconciling the data with `ReadingType`, deciding where readings attach to `LiturgicalDay`, and resolving the Year I/II division and movable-Holy-Day override at request time.

A first design draft got two things wrong, both caught by an independent review that checked the actual JSON on disk rather than trusting the draft's assumptions:

### `ReadingType.FirstLesson`/`SecondLesson` don't reliably mean OT/NT for the Daily Office

The first draft assumed — following the Sunday lectionary's pattern, where `FirstLesson` is OT/Apocrypha 100% of the time across ~500 sampled entries — that the Daily Office Lessons JSON's `*FirstLesson` fields would be similarly OT-reliable. They aren't: 22 of 366 dates have an NT reading in a `*FirstLesson` field (mostly major feasts where both lessons of a pair land in the NT — Acts, Galatians, Ephesians, James). The same is true even within the movable-Holy-Day data: Maundy Thursday's Evening Prayer `firstLesson` is `1 Corinthians 10:1-22`. The Lessons JSON's own `_comment` already warned that "Morning"/"Evening" name the printed *column*, not the office; it turns out "First"/"Second" is equally positional, not a content promise — for the Daily Office specifically, though not for Sunday.

### The 30-day Psalter file's actual shape didn't match the first draft's assumption

The first draft assumed a `{psalms: [...]}` wrapper matching the 60-day file. The actual `acna-bcp2019-daily-office-psalter-30day.json` is raw arrays of mixed `int`/`string` tokens keyed by day-of-month (`"6": {"morningPrayer": [30, 31], "eveningPrayer": [32, 33, 34]}`, `"24": {"eveningPrayer": ["119:1-32"]}}`) — a different shape requiring its own parser, not a shared one with the 60-day file.

## Decisions

### `LiturgicalDay.DailyOffice`, a new property — not a reuse of `Readings`

New record `DailyOfficeReadings { required LiturgicalService MorningPrayer; required LiturgicalService EveningPrayer; }`. `LiturgicalDay.Readings` (the Sunday/Holy Day Eucharist lectionary) is untouched.

Rejected: appending "Morning Prayer"/"Evening Prayer" as two more `LiturgicalService` entries into the existing `Readings` list. That would silently break every `day.Readings[0]`-style consumer (which assumes index 0 is the Eucharist service) and works against issue #9's own acceptance criterion that Daily Office readings must not interfere with Sunday Eucharist readings — a separate property makes that structural rather than a naming convention callers have to respect.

`DailyOffice` is `required` and always populated: unlike the Eucharist, which is genuinely absent on most ordinary weekdays, the Daily Office is prayed every day of the year.

### `ReadingType` reuse, corrected for the OT/NT finding above

- **Regular calendar-date grid** (each office gets exactly one lesson — see the Year I/II decision below): `ReadingType.MorningPrayer` for Morning Prayer's lesson, `ReadingType.EveningPrayer` for Evening Prayer's — both enum values already existed, added speculatively for this feature and unused until now. They name the reading by *which office it's read at*, carrying no OT/NT implication, which is the honest choice given the finding above.
- **Movable Holy Days** (each office gets two fully-resolved lessons): `ReadingType.FirstLesson`/`SecondLesson`, reused from the Eucharist model. This case genuinely parallels the Eucharist's two-lesson-per-service shape (two readings need distinguishing in one list), and per ADR 0002's already-established "slot, not content" principle, reuse is consistent even though the content isn't always OT/NT-ordered here either.
- `ReadingType.Psalm` is unchanged in both cases.

One visible consequence: a consumer filtering purely by `Type` won't get uniform behavior across all 366 dates — the 7 movable-Holy-Day dates use `FirstLesson`/`SecondLesson` while the other 359 use `MorningPrayer`/`EveningPrayer`. This is intentional; it reflects a genuine structural difference in the BCP source (fully-resolved proper tables vs. a continuous reading requiring division), not an inconsistency to paper over.

### Year I/II division: pure calendar-year parity, exactly one lesson per office

Per the BCP's own front matter (quoted in issue #7's Design note): odd calendar years (Year I) use the day's `MorningFirstLesson`+`MorningSecondLesson` pair; even years (Year II) use `EveningFirstLesson`+`EveningSecondLesson`. Whichever pair is active, its first lesson is read at Morning Prayer and its second at Evening Prayer — one lesson per office, not two. This is `date.Year % 2` parity, **independent of `LiturgicalWeek.LectionaryYear`** (the Sunday Eucharist's A/B/C, Advent-anchored cycle) — the two must never be conflated, exactly the trap the Design note originally flagged.

### Movable Holy Days: exact Easter-offset match only, full override

`acna-bcp2019-daily-office-movable-holy-days.json`'s 7 entries carry a complete `firstLesson`+`secondLesson`+`psalms` for each office already — no division needed, these are fully-resolved proper tables, not part of the calendar-date grid. Resolution: compute `easter = EasterCalculator.GetEaster(date.Year)`, compare `date.DayNumber - easter.DayNumber` against the 7 `easterOffsetDays` values. On a match, the movable entry's readings **replace** the grid lookup entirely for that date — nothing from the regular grid is mixed in.

The match must be against the 7 discrete offsets only, never a date range — the front matter's "the readings for it, and through Easter Day, replace those appointed for the Calendar dates" reads ambiguously, and this project has already misread a Daily Office marker once before (ADR 0004's correction of the †/‡ semantics). `AcnaDailyOfficeLectionaryTests` includes an explicit case proving an ordinary Eastertide weekday does not trigger the Easter Day override.

### Psalms: 30-day cycle only, 60-day cycle as a day-31 fallback, no `AlternateCitations` overload

`Citation` is always the 30-day cycle's value (the BCP's own front matter calls it the "traditional" cycle), formatted `"Ps {tokens}"` to match `AcnaSundayLectionary`'s existing abbreviation convention. On day 31 — the 30-day cycle has no entry that day, since the BCP grants free discretion there rather than naming fixed Psalms (see issue #30) — the 60-day cycle's value is used as the sole `Citation` instead, so `GetDay` never returns a missing Psalm reading.

The first draft planned to expose the 60-day cycle as a second option via `AlternateCitations`. Dropped: `AlternateCitations` already carries two different meanings after ADR 0004 (named alternative options, and optional abbreviations); the 30-day/60-day choice is a standing parish preference, not a per-occasion alternate, and stretching the field a third way would make its meaning harder to pin down rather than easier. Issue #33 tracks exposing the 60-day cycle as a real, chooseable option instead (a parameter, not a citation variant).

`TranslationCode` is always `"NCP"` on every Psalm reading, mirroring `AcnaSundayLectionary`'s existing Sunday-lectionary convention (all 247 Sunday Psalm entries carry NCP, not just verse-range ones) — not conditional on the JSON's per-entry `v` marker, which only flags citations the BCP additionally called out for verse-numbering awareness.

## Consequences

- `ReadingType.FirstLesson`/`SecondLesson` and `MorningPrayer`/`EveningPrayer` now mean different things depending on which service they appear in (Sunday Eucharist vs. Daily Office grid vs. Daily Office movable Holy Day) — all four values were already documented as slot-not-content per ADR 0002, but this widens how differently "slot" can cash out across contexts. A consumer building UI around `ReadingType` needs to know which service it's rendering, not just switch on the enum value alone.
- `AcnaDailyOfficeLectionary` computes `EasterCalculator.GetEaster(date.Year)` independently on every `GetReadings` call, following the same "everyone computes Easter inline" convention already used by `SeasonResolver`, `AcnaFeastCatalog`, and `AcnaSundayLectionary` — no new shared Easter-caching mechanism was introduced.
- The 60-day Psalter cycle and the Christmas Day `alternatePsalms` ("19 or 45") field in the 60-day JSON are not consumed by this design at all outside the day-31 fallback path (and no day-31 date is ever Dec 25, so no interaction). Both remain available for #33 to build on.
- `LiturgicalDay.DailyOffice` is `required`, which means it must be set in `AcnaBcp2019Calendar.GetDay`'s initial object initializer rather than a later `with` expression (a `with` cannot newly satisfy a `required` member) — unlike `Readings`, which genuinely needs a second pass because it depends on the already-constructed day's `Week`/`Feast`.
