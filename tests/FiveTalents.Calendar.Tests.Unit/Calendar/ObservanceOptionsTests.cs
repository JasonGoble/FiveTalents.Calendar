using FiveTalents.Calendar.Calendar;

namespace FiveTalents.Calendar.Tests.Unit.Calendar;

/// <summary>
/// Verifies <see cref="AcnaBcp2019Calendar.GetPossibleEucharistObservances"/> against the
/// worked examples in ADR 0008 — a precedence-ordered list of rubrically-possible Eucharist
/// observances, rather than a single resolved answer. Closes the Eucharist half of #30.
/// </summary>
public sealed class ObservanceOptionsTests
{
    private readonly AcnaBcp2019Calendar _calendar = new();

    // ── No competing Holy Day ────────────────────────────────────────────────

    [Fact]
    public void OrdinaryWeekday_NoHolyDay_SingleSeasonOption()
    {
        var options = _calendar.GetPossibleEucharistObservances(new DateOnly(2026, 6, 9));

        var option = Assert.Single(options);
        Assert.Null(option.Feast);
        Assert.Equal(ObservancePrecedence.Prescribed, option.Precedence);
        Assert.Null(option.RubricNote);
    }

    // ── A Principal Feast owns its own Sunday (season key == feast key) ──────

    [Fact]
    public void TrinitySunday_FeastOwnsItsOwnSlot_SingleOptionWithFeastAttached()
    {
        var options = _calendar.GetPossibleEucharistObservances(new DateOnly(2026, 5, 31));

        var option = Assert.Single(options);
        Assert.Equal("Trinity Sunday", option.Feast!.Name);
        Assert.Equal(ObservancePrecedence.Prescribed, option.Precedence);
    }

    // ── A Feast with no lectionary entry of its own still gets attributed ────
    // Neither "Christmas Day" nor "The Epiphany of Our Lord Jesus Christ" appear in
    // AcnaSundayLectionary's feast-key map; their propers are sourced via the season path
    // instead. Feast attribution must not depend on whether that path finds any readings —
    // see the Epiphany case, which has none on a non-Sunday date.

    [Fact]
    public void ChristmasDay_SourcedViaSeasonPath_StillAttachesFeast()
    {
        var options = _calendar.GetPossibleEucharistObservances(new DateOnly(2026, 12, 25));

        var option = Assert.Single(options);
        Assert.Equal("Christmas Day", option.Feast!.Name);
        Assert.NotEmpty(option.Services);
    }

    [Fact]
    public void EpiphanyOnAWeekday_NoLectionaryEntryEitherPath_StillAttachesFeastWithEmptyServices()
    {
        // 2026-01-06 is a Tuesday — "Days After Epiphany" carry no Sunday proper of their
        // own, and Epiphany itself isn't in the feast-key map. Feast must still be reported
        // (matching pre-ADR-0008 behavior) even though there is no reading to go with it.
        var options = _calendar.GetPossibleEucharistObservances(new DateOnly(2026, 1, 6));

        var option = Assert.Single(options);
        Assert.Equal("The Epiphany of Our Lord Jesus Christ", option.Feast!.Name);
        Assert.Empty(option.Services);
    }

    // ── A Red-Letter Day on its own weekday: Prescribed + CommonPractice ─────
    // BCP 2019 p.688: Red-Letter Holy Days are BCP-directed. Skipping one for the ordinary
    // weekday reading isn't rubric-sanctioned, but is real, practiced deviation (Jason,
    // 2026-07-10: "there are churches that do not celebrate the individual saints due to
    // theological differences... give them more information and let them decide").

    [Fact]
    public void RedLetterDayOnItsOwnWeekday_PrescribedFeast_PlusCommonPracticeAlternative()
    {
        // 2026-11-30 is a Monday — Andrew the Apostle's fixed date, no Sunday involved.
        var options = _calendar.GetPossibleEucharistObservances(new DateOnly(2026, 11, 30));

        Assert.Equal(2, options.Count);

        var prescribed = options[0];
        Assert.Equal(ObservancePrecedence.Prescribed, prescribed.Precedence);
        Assert.Equal("Andrew the Apostle", prescribed.Feast!.Name);

        var commonPractice = options[1];
        Assert.Equal(ObservancePrecedence.CommonPractice, commonPractice.Precedence);
        Assert.Null(commonPractice.Feast);
    }

    // ── A Holy Day on an ordinary Sunday: two co-equal Prescribed options ────
    // BCP 2019 p.689: "may be observed on that Sunday or transferred to the nearest
    // following weekday" — the rubric explicitly grants the choice, so both options are
    // Prescribed; neither is a CommonPractice deviation.

    [Fact]
    public void HolyDayOnOrdinarySunday_BothOptionsPrescribed()
    {
        // 2026-10-18: Luke the Evangelist falls on an ordinary Sunday.
        var options = _calendar.GetPossibleEucharistObservances(new DateOnly(2026, 10, 18));

        Assert.Equal(2, options.Count);
        Assert.All(options, o => Assert.Equal(ObservancePrecedence.Prescribed, o.Precedence));

        Assert.Equal("Luke the Evangelist and Companion of Paul", options[0].Feast!.Name);
        Assert.Null(options[1].Feast);
    }

    // ── A Holy Day yielding to an Advent/Lent/Easter Sunday: single option, RubricNote ──
    // BCP 2019 p.689: inside Advent, Lent, or Easter the rubric grants no choice at all —
    // the Sunday's own propers govern outright. No CommonPractice alternative is offered
    // (Jason: "Eucharistically... CommonPractice does not need to appear at all"); instead
    // RubricNote carries the citation so the information isn't simply lost.

    [Fact]
    public void HolyDayYieldsToAdventSunday_SingleOptionWithRubricNote()
    {
        // 2025-11-30: Andrew the Apostle coincides with the First Sunday of Advent.
        var options = _calendar.GetPossibleEucharistObservances(new DateOnly(2025, 11, 30));

        var option = Assert.Single(options);
        Assert.Null(option.Feast);
        Assert.Equal(ObservancePrecedence.Prescribed, option.Precedence);
        Assert.NotNull(option.RubricNote);
        Assert.Contains("Andrew the Apostle", option.RubricNote);
        Assert.Contains("p.689", option.RubricNote);
        Assert.Contains("transferred", option.RubricNote);
        Assert.Equal("Andrew the Apostle", option.YieldedFeast?.Name);
    }

    // ── YieldedFeast is populated across all three yield seasons, not just Advent ────
    // #30: exposes the yielded Feast as structured data (in addition to RubricNote's free
    // text) so a consumer can identify it without parsing a sentence. Deliberately does not
    // compute or suggest a transfer date — see ADR 0010.

    [Theory]
    [InlineData(2025, 11, 30, "Andrew the Apostle")]              // Advent Sunday 1
    [InlineData(2023, 3, 19, "Joseph, the Guardian of Jesus")]    // Lent Sunday 4
    [InlineData(2021, 4, 25, "Mark the Evangelist")]              // Easter Sunday 4
    public void HolyDayYieldsToSunday_YieldedFeastIsPopulated(int y, int m, int d, string expectedFeastName)
    {
        var options = _calendar.GetPossibleEucharistObservances(new DateOnly(y, m, d));

        var option = Assert.Single(options);
        Assert.Equal(expectedFeastName, option.YieldedFeast?.Name);
    }

    // ── YieldedFeast stays null everywhere else ──────────────────────────────

    [Fact]
    public void HolyDayOnOrdinarySunday_YieldedFeastIsNullOnBothOptions()
    {
        var options = _calendar.GetPossibleEucharistObservances(new DateOnly(2026, 10, 18));

        Assert.All(options, o => Assert.Null(o.YieldedFeast));
    }

    [Fact]
    public void RedLetterDayOnItsOwnWeekday_YieldedFeastIsNullOnBothOptions()
    {
        var options = _calendar.GetPossibleEucharistObservances(new DateOnly(2026, 11, 30));

        Assert.All(options, o => Assert.Null(o.YieldedFeast));
    }

    [Fact]
    public void HolyWeekWeekday_NoCollision_YieldedFeastIsNull()
    {
        var options = _calendar.GetPossibleEucharistObservances(new DateOnly(2026, 3, 30));

        var option = Assert.Single(options);
        Assert.Null(option.YieldedFeast);
    }

    // ── Holy Week: single option, no alternative ──────────────────────────────
    // BCP 2019 p.689: "No holy day or observance can replace the fixed propers for...
    // Holy Week." A fixed feast that would otherwise collide (e.g. the Annunciation on
    // Monday of Holy Week in 2024) is suppressed upstream in AcnaFeastCatalog, so
    // AcnaFeastCatalog.GetSuppressedFixedHolyDay reports it separately for RubricNote/
    // YieldedFeast to surface here. See ADR 0011. Closes #47.

    [Theory]
    [InlineData(2026, 3, 30, "Monday of Holy Week")] // no collision this year
    [InlineData(2024, 3, 25, "Monday of Holy Week")] // Annunciation would collide, suppressed upstream
    public void HolyWeekWeekday_SingleOptionOnly(int y, int m, int d, string expectedFeast)
    {
        var options = _calendar.GetPossibleEucharistObservances(new DateOnly(y, m, d));

        var option = Assert.Single(options);
        Assert.Equal(expectedFeast, option.Feast!.Name);
        Assert.Equal(ObservancePrecedence.Prescribed, option.Precedence);
    }

    [Fact]
    public void HolyWeekWeekday_NoCollision_RubricNoteIsNull()
    {
        var options = _calendar.GetPossibleEucharistObservances(new DateOnly(2026, 3, 30));

        var option = Assert.Single(options);
        Assert.Null(option.RubricNote);
    }

    [Fact]
    public void HolyWeekWeekday_CollisionSuppressed_RubricNoteExplainsIt()
    {
        // 2024-03-25: the Annunciation's fixed date, suppressed by Monday of Holy Week.
        var options = _calendar.GetPossibleEucharistObservances(new DateOnly(2024, 3, 25));

        var option = Assert.Single(options);
        Assert.NotNull(option.RubricNote);
        Assert.Contains("Annunciation", option.RubricNote);
        Assert.Contains("p.689", option.RubricNote);
    }

    // ── YieldedFeast/RubricNote for the Holy Week/Easter Week suppression case ───────
    // #47: AcnaFeastCatalog.GetSuppressedFixedHolyDay exposes what GetHolyDays discarded,
    // so this case is annotated the same way the Advent/Lent/Easter Sunday yield already
    // is (ADR 0010). See ADR 0011.

    [Theory]
    [InlineData(2024, 3, 25)] // Monday of Holy Week
    [InlineData(2043, 3, 25)] // Wednesday of Holy Week
    [InlineData(2160, 3, 25)] // Tuesday of Easter Week — no moveable Feast that day
    public void HolyWeekOrEasterWeekSuppression_YieldedFeastIsPopulated(int y, int m, int d)
    {
        var options = _calendar.GetPossibleEucharistObservances(new DateOnly(y, m, d));

        var option = Assert.Single(options);
        Assert.Equal("The Annunciation of Our Lord Jesus Christ to the Virgin Mary", option.YieldedFeast?.Name);
    }

    // ── GetDay derives from the first Prescribed option ──────────────────────

    [Theory]
    [InlineData(2026, 11, 30, "Andrew the Apostle")]  // Prescribed + CommonPractice — first wins
    [InlineData(2026, 10, 18, "Luke the Evangelist and Companion of Paul")] // two Prescribed — first wins
    [InlineData(2025, 11, 30, null)]                  // yields — Feast is null
    public void GetDay_FeastAndReadings_MatchFirstPrescribedOption(int y, int m, int d, string? expectedFeastName)
    {
        DateOnly date = new DateOnly(y, m, d);
        var options = _calendar.GetPossibleEucharistObservances(date);
        var day = _calendar.GetDay(date);

        var firstPrescribed = options.First(o => o.Precedence == ObservancePrecedence.Prescribed);
        Assert.Equal(expectedFeastName, day.Feast?.Name);

        // Compare citations rather than the LiturgicalService/LectionaryReading records
        // directly — each call rebuilds the JSON-backed object graph from scratch, and
        // record equality isn't deep for list-typed fields like AlternateCitations.
        var expectedCitations = firstPrescribed.Services.SelectMany(s => s.Readings).Select(r => r.Citation);
        var actualCitations = day.Readings.SelectMany(s => s.Readings).Select(r => r.Citation);
        Assert.Equal(expectedCitations, actualCitations);
    }
}
