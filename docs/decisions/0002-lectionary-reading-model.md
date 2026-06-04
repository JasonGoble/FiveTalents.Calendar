# 0002 — Lectionary Reading Model: Services, Reading Types, and Alternate Citations

**Status:** Accepted

## Context

Anglican liturgy is more structurally complex than a flat list of Bible citations per day. Several forces drove the data model:

### Multiple services on one day

Palm Sunday has two complete, distinct liturgies within the same calendar day:

- **Liturgy of the Palms** — the entrance procession; year-keyed Gospel (Matthew/Mark/Luke) and Psalm 118
- **Liturgy of the Word** — the principal service; OT lesson, Psalm 22, Epistle, and the Passion Gospel

A flat `IReadOnlyList<LectionaryReading>` on `LiturgicalDay` cannot represent this without colliding on `ReadingType` (two Gospels, two Psalms). The Great Vigil of Easter is an even more extreme case: twelve First Lesson readings in a single night (tracked for future resolution in issue #12).

### Reading slot vs. reading content

The liturgical structure of the Eucharist has named positions — First Lesson, Psalm, Second Lesson, Gospel — regardless of what book fills them. During Eastertide, the First Lesson slot is filled by Acts rather than an Old Testament book; this is deliberate BCP practice, not a data error. A type named `OldTestament` would be literally wrong for half of Easter week.

### Multiple alternate citations

The BCP frequently provides shorter alternatives to long passages (e.g., a full Passion Gospel with a shorter option). For most readings this is zero or one alternate. The Palm Sunday Psalm has three options. A single `AlternateCitation: string?` property cannot model the three-option case.

### Translation and versification differences

The ACNA BCP 2019 draws from multiple translation traditions. Psalms use the **New Coverdale Psalter (NCP)**, which follows Septuagint/Vulgate numbering in some cases — meaning a consumer cannot assume that "Ps 22" in a NCP citation refers to the same text as "Ps 22" in an ESV citation. An optional `TranslationCode` field allows consumers to resolve the correct edition without inspecting the citation string.

## Decisions

### `LiturgicalService` wrapper

`LiturgicalDay.Readings` is `IReadOnlyList<LiturgicalService>` rather than a flat list of readings. Each `LiturgicalService` has:
- `string? Name` — null for single-service days; named when the day has multiple services (e.g. "Liturgy of the Palms", "Liturgy of the Word")
- `IReadOnlyList<LectionaryReading> Readings`

Most days return one unnamed service. Palm Sunday returns two named services. This structure is extensible to the Great Vigil and any future tradition with multi-service days.

### `ReadingType` as liturgical slot, not content descriptor

The four reading positions are named after their liturgical role:

| Enum value | BCP label | Notes |
|---|---|---|
| `FirstLesson` | "The First Lesson" | OT most of the year; Acts during Eastertide |
| `Psalm` | "The Psalm" | Always a Psalm or canticle |
| `SecondLesson` | "The Second Lesson" | Epistle, Acts, or Revelation |
| `Gospel` | "The Gospel" | Always one of the four Gospels |

`OldTestament` and `Epistle` were rejected because they describe content rather than slot, and both are factually wrong on specific days in the calendar.

### `AlternateCitations: IReadOnlyList<string>`

Replaces `AlternateCitation: string?`. An empty list means no alternatives exist. A single-element list is the common case (one shorter option). A multi-element list handles the Palm Sunday Psalm (and any future readings with more than one alternative). The JSON parser accepts `"alternate"` as either a string (single) or an array (multiple) for backward compatibility with existing data.

### `TranslationCode: string?`

An optional string on `LectionaryReading` identifying the translation or versification in use. Null means the default (ESV for prose; the field is present but not required). All 247 Psalm entries carry `"translationCode": "NCP"`. A companion reference endpoint (issue #11) will resolve codes to human-readable edition names.

## Consequences

- Consumers must index into `Readings[0]` for the common single-service case, or iterate by `Name` for multi-service days.
- `ReadingType` values are strings in the JSON API (via `JsonStringEnumConverter`); consumers must use `"FirstLesson"` and `"SecondLesson"`, not `"OldTestament"` and `"Epistle"`.
- The Great Vigil currently returns one service with twelve `FirstLesson` readings — a known limitation tracked in issue #12.
- Any reading whose `TranslationCode` is `"NCP"` may carry a Psalm number that differs from Hebrew/Masoretic numbering; consumers resolving Psalm citations should account for this.
