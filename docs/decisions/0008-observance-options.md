# 0008 — Possible Observances: Ranked Options Instead of a Single Answer

**Status:** Accepted

## Context

ADR 0006/0007 established that the engine should enforce mandatory rubrics ("cannot") unconditionally but must not unilaterally resolve permissive ones ("may") — #43's first pass over-reached by auto-transferring a yielded Holy Day to a specific weekday, which Jason walked back because it removed a real pastoral choice.

That raised the actual product question (2026-07-10 design conversation): what does this library exist to do? Jason's answer, in two user stories:

1. *As an officiant, given a date, give me the possible Eucharist observances (and their readings), ordered by rubrically correct precedence.*
2. *As an officiant, given a date, give me the possible Morning/Evening Prayer observances (and their readings), ordered by rubrically correct precedence.*

And the underlying goal: correct rubrical data, consumed by *any* rendering platform (a future liturgical-bulletin/script generator is the concrete example), which makes its own presentation decisions rather than being locked into one this library decided for it. That reframes every discretionary-rubric question raised in #30: instead of the engine picking one answer, it returns the *ranked set of rubric-valid answers* and lets the caller choose.

We compared this against [dailyoffice2019.com](https://www.dailyoffice2019.com) (open source: github.com/blocher/dailyoffice2019), a mature, widely-used ACNA BCP 2019 daily-office renderer, and found it doesn't expose this kind of choice at all — its ~25 settings are almost entirely about *how to render an already-decided office* (translation, language register, abbreviated vs. full), not *which office governs a date*. That's a different audience (individual devotional use vs. an officiant preparing worship) and not a wheel worth reinventing; see [[reference-dailyoffice2019-settings]] for the settings vocabulary if this library ever grows into that rendering-preference space.

## Decision

### `ObservanceOption` and `GetPossibleEucharistObservances`

```csharp
public enum ObservancePrecedence
{
    Prescribed,      // rubric-sanctioned — the sole answer, or one of several the rubric explicitly grants as equally valid
    CommonPractice,  // not rubric-sanctioned, but a real deviation some churches practice — surfaced, not hidden
}

public sealed record ObservanceOption
{
    public FeastDay? Feast { get; init; }                         // null = the season's own propers, no named feast
    public required ObservancePrecedence Precedence { get; init; }
    public required IReadOnlyList<LiturgicalService> Services { get; init; }
    public string? RubricNote { get; init; }                       // present when an option's absence needs explaining, not when it's just absent
}

// on ILiturgicalCalendar
IReadOnlyList<ObservanceOption> GetPossibleEucharistObservances(DateOnly date);
```

This is Eucharist-only for now. A general `GetPossibleObservances(date, ServiceKind)` is the anticipated eventual shape once the Morning/Evening Prayer side has real data to back it (see #46) — not built ahead of that, per "don't design for hypothetical future requirements."

### The two-tier model, and where the line falls

Working through concrete cases with Jason (2026-07-10) collapsed what started as a three-tier design (`Prescribed`/`Permitted`/`CommonPractice`) into two: "the rubric explicitly grants this as a valid choice" and "it isn't rubric-sanctioned but happens anyway" are the only distinction that matters — a rubric-granted *choice between two things* just means two `Prescribed` items, not a `Prescribed`/`Permitted` split.

The rule that determines which tier an alternative gets, worked out from Jason's answers on Andrew (Red-Letter Day, transfer target: `Prescribed`, alternatives `CommonPractice`) and All Saints (Principal Feast, the Sunday after Nov 1: both `Prescribed`, no lesser tier): **the BCP's Sunday-collision rubric (p.689, "may be observed on that Sunday or transferred") only grants discretion for collisions *on a Sunday*.** So:

- A Holy Day colliding with an ordinary **Sunday** → both the Holy Day and that Sunday's own propers are `Prescribed` — the rubric explicitly grants the choice.
- A Holy Day on its own **weekday**, with no Sunday involved → the Holy Day is `Prescribed` (Red-Letter Days are BCP-directed); the ordinary weekday-borrowed reading, if a congregation chooses it instead, is `CommonPractice` — nothing in the rubric sanctions skipping a Red-Letter Day, it just happens (Jason: "there are churches that do not celebrate the individual saints due to theological differences... give them more information and let them decide").
- A Principal Feast never gets a `CommonPractice` alternative — Rule 1 (ADR 0006) is absolute, and there's no evidence of real-world deviation from a Principal Feast the way there is for an ordinary Red-Letter Day.
- A non-Principal Holy Day yielding to an Advent/Lent/Easter Sunday (#45/ADR 0007), and a fixed Holy Day suppressed by the Holy Week/Easter Week protection (ADR 0006), both stay single-item `Prescribed` — no `CommonPractice` alternative — per Jason: "Eucharistically... CommonPractice does not need to appear at all." Instead, `RubricNote` carries the citation, so the information isn't lost, just not offered as a selectable option: "though my tendency is still to give more information than less... resolve this by a reference to the rubric in the RubricNote field... an adequate middle ground."

### Known limitation: `RubricNote` for the Holy Week/Easter Week case

`RubricNote` is populated for the Advent/Lent/Easter-Sunday-yield case, because `GetPossibleEucharistObservances` still sees the colliding Holy Day before deciding to exclude it. It is **not** yet populated for the Holy Week/Easter Week case, because `AcnaFeastCatalog.GetHolyDays` suppresses a colliding fixed Holy Day before this method ever sees it — by the time `GetPossibleEucharistObservances` runs, there's nothing left to cite. Surfacing that note would need `AcnaFeastCatalog` to expose what it suppressed, purely for annotation purposes; tracked as #47 rather than expanded into this change (the collision itself is rare — roughly ten occurrences between 2020 and 2160 across the whole fixed Holy Day catalog).

### `GetDay` becomes a derived view

`LiturgicalDay.Feast` and `.Readings` no longer carry their own precedence logic (`ResolveFeast` in `AcnaBcp2019Calendar`, `ResolveKey` in `AcnaSundayLectionary`). Both are now the first `Prescribed` item of `GetPossibleEucharistObservances(date)` — same single source of truth the new method uses, closing off the exact class of inconsistency #43 was (two independently-maintained precedence computations disagreeing with each other). Verified this preserves every existing observable behavior by construction (worked through each precedence case in ADR 0006/0007 against the new derivation) and by the full existing test suite passing unchanged.

## Worked examples (== `ObservanceOptionsTests` scenarios)

| Date/case | Options |
|---|---|
| Ordinary date, no Holy Day | 1 `Prescribed` (season propers) |
| Trinity Sunday / Easter Day (Feast *is* the season's own Sunday) | 1 `Prescribed`, `Feast` populated |
| Andrew on his own fixed weekday | `Prescribed` (Andrew) + `CommonPractice` (ordinary weekday) |
| Luke coincides with an ordinary Sunday | 2 `Prescribed` (Luke, that Sunday's Proper) |
| Andrew coincides with Advent Sunday 1 | 1 `Prescribed` (Advent 1), `RubricNote` citing p.689 |
| Holy Week/Easter Week fixed weekday | 1 `Prescribed` only, no `RubricNote` yet (see limitation above) |

## Consequences

- `ObservancePrecedence`/`ObservanceOption` live in `FiveTalents.Calendar.Calendar`, alongside `LiturgicalDay`.
- The Morning/Evening Prayer side of the officiant's two user stories is blocked on #46 (fixed-Holy-Day Daily Office data doesn't exist yet); `GetPossibleEucharistObservances` intentionally does not attempt it.
- The "where does a yielded Holy Day actually get observed" question (#30, #43's original over-reach) remains genuinely open — this ADR does not resolve it. `GetPossibleEucharistObservances` only reasons about the date it's given; it does not look forward or backward across dates to find a transfer target.
- All Saints' additional Sunday observance (#42) also remains unimplemented here — it requires a new date computation (which Sunday is "the Sunday following Nov 1," including the ambiguity of what that means when Nov 1 already is a Sunday) not yet designed.
