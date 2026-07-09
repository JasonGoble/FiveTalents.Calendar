# 0004 — Daily Office Marker Semantics: †, ‡, and NCP Versification

**Status:** Accepted

## Context

The BCP 2019 Daily Office Lectionary uses two inline symbols and one versification convention that don't map onto any existing model concept. Issue #20 shipped the Lesson citations with both markers preserved verbatim inline in the citation string (e.g. `"Joshua 22 † 7-31"`), deferring their real semantics to a follow-up (#28). Extracting the Psalter data for #28 required reading the DOCX's own front matter, which corrected two assumptions #20's audit had made:

### The dagger (†) is not Easter-dependent

The front matter states plainly: "The dagger symbol (†) indicates a way to abbreviate a longer chapter if desired." It marks an optional shorter form of the same reading — e.g. `Nehemiah 12 † 27-47` means the full chapter (Nehemiah 12) is the default reading, and verses 27–47 are a permitted abbreviation. This is unrelated to Easter or any other date computation; #20's audit doc incorrectly characterized it as an "Easter-dependent substitution," which actually describes the double dagger instead.

### The double dagger (‡) marks a print-navigation pointer, not calendar-date content

‡ appears in the margin of the printed calendar-date tables at exactly the *earliest possible date* a movable Holy Day could fall (Feb 4 for Ash Wednesday, Mar 19 for Maundy Thursday–Easter Day, Apr 30 for Ascension–Pentecost — three positions total, not four as an earlier, mistaken count suggested). Each position points to a small fixed proper-lessons table printed elsewhere in the same document. Per the front matter: "The readings for it, and through Easter Day, replace those appointed for the Calendar dates." The marker itself carries no data — it only tells the reader where to look up the table for whichever year they're using the book in.

### The 60-day Psalter's NCP versification suffix

Verse-range citations in the 60-day Psalter cycle carry a literal trailing `v` in the source DOCX (e.g. `"78:41-73v"`), marking New Coverdale Psalter (NCP) versification — the same convention already modeled by `LectionaryReading.TranslationCode` for the Sunday lectionary's Psalms (see ADR 0002).

## Decisions

### † → `AlternateCitations` (reuse, not a new field)

The dagger's optional-abbreviation citation becomes an `AlternateCitations` entry rather than staying inline or gaining a dedicated field:

```json
{ "type": "MorningFirstLesson", "citation": "Nehemiah 12", "alternate": "Nehemiah 12:27-47" }
```

This was a genuine fork (confirmed with the project owner): a new `AbbreviatedCitation` field would keep "distinct alternate options" (Palm Sunday's 3-option Psalm) and "abbreviated form of the primary reading" conceptually separate, but at the cost of a near-duplicate field for a distinction most consumers won't need to treat differently. Reusing `AlternateCitations` broadens its meaning slightly but avoids that duplication, and is consistent with ADR 0002's original motivation for the field — moving structured semantics out of the citation string rather than embedding them inline. Issue #20's already-merged Lesson data (182 dagger citations) was revised in place to match, since nothing consumes it yet.

### ‡ → a separate Easter-relative override table, not a per-date flag

The movable Holy Days (Ash Wednesday, Maundy Thursday, Good Friday, Holy Saturday, Easter Day, Ascension, Pentecost) are staged in `acna-bcp2019-daily-office-movable-holy-days.json`, keyed by name with an `easterOffsetDays` field, not folded into the calendar-date grid. This mirrors the existing pattern in `AcnaFeastCatalog`/`SeasonResolver`, which already compute several of these exact same dates via Easter offsets (Ash Wednesday `-46`, Holy Saturday `-1`, Pentecost `+49`). No ‡ flag is stored anywhere — the marker was purely a print-layout aid with no runtime meaning, and every offset needed is already computed elsewhere in the codebase by the same mechanism.

### 60-day Psalter versification → reuse `TranslationCode`, not new markup

The trailing `v` is dropped from the citation text and represented as `"translationCode": "NCP"` on that office's Psalm entry, exactly as the Sunday lectionary already does — no new field, no inline character.

## Consequences

- `LectionaryReading.AlternateCitations` now represents two related-but-distinct BCP concepts (named alternative options, and optional abbreviations) under one field. A consumer rendering "alternate" readings in UI may want to distinguish these cases by inspecting whether the alternate is a subset of the primary citation's range — that distinction is not carried in the data itself.
- Resolving a movable Holy Day's readings for a given year requires computing Easter first and checking whether the current date matches one of the seven offsets in `acna-bcp2019-daily-office-movable-holy-days.json` — this resolution logic does not exist yet and is #9's responsibility, alongside deciding how it interacts with the Year I/II lesson-division rule (see the Design note in issue #7).
- The 60-day Psalter JSON's single documented exception to the "whole-psalms-list or one verse-range citation" shape is Christmas Day (`12-25`) Morning Prayer, where the BCP names two specific alternative Psalms ("19 or 45") rather than a list to read together — represented as `psalms: ["19"], alternatePsalms: ["45"]`.
- Two known instances of open-ended discretionary rubrics (the 30-day Psalter's day-31 substitution, and the Psalter's general "may be reduced in number" allowance) were deliberately excluded from the structured data rather than solved here — tracked in issue #30.
