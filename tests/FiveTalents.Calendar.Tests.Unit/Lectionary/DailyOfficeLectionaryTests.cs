using FiveTalents.Calendar.Calendar;
using FiveTalents.Calendar.Lectionary;

namespace FiveTalents.Calendar.Tests.Unit.Lectionary;

/// <summary>
/// Verifies the Daily Office readings model: the Year I/II calendar-year-parity
/// division, the movable-Holy-Day override, and the Psalm-cycle fallback. See
/// docs/decisions/0005-daily-office-readings-model.md for the design rationale.
/// </summary>
public sealed class DailyOfficeLectionaryTests
{
    private readonly AcnaBcp2019Calendar _calendar = new();

    // ── Year I/II division ───────────────────────────────────────────────────

    [Fact]
    public void DailyOffice_Jan1_OddYear_UsesMorningColumnPair()
    {
        // 2025 is an odd calendar year (Year I) — uses the MorningFirstLesson/
        // MorningSecondLesson pair, split first→MP, second→EP.
        var day = _calendar.GetDay(new DateOnly(2025, 1, 1));

        var mpLesson = day.DailyOffice.MorningPrayer.Readings.First(r => r.Type == ReadingType.MorningPrayer);
        Assert.Equal("Genesis 1", mpLesson.Citation);

        var epLesson = day.DailyOffice.EveningPrayer.Readings.First(r => r.Type == ReadingType.EveningPrayer);
        Assert.Equal("John 1:1-28", epLesson.Citation);
    }

    [Fact]
    public void DailyOffice_Jan1_EvenYear_UsesEveningColumnPair()
    {
        // 2026 is an even calendar year (Year II) — uses the EveningFirstLesson/
        // EveningSecondLesson pair instead, for the SAME calendar date.
        var day = _calendar.GetDay(new DateOnly(2026, 1, 1));

        var mpLesson = day.DailyOffice.MorningPrayer.Readings.First(r => r.Type == ReadingType.MorningPrayer);
        Assert.Equal("Galations 1", mpLesson.Citation);

        var epLesson = day.DailyOffice.EveningPrayer.Readings.First(r => r.Type == ReadingType.EveningPrayer);
        Assert.Equal("Luke 2:8-21", epLesson.Citation);
    }

    // ── Movable Holy Days ─────────────────────────────────────────────────────

    [Fact]
    public void DailyOffice_AshWednesday_UsesMovableHolyDayOverride()
    {
        // Ash Wednesday 2026 = Easter(2026-04-05) - 46 days = Feb 18, 2026.
        var day = _calendar.GetDay(new DateOnly(2026, 2, 18));

        var mp = day.DailyOffice.MorningPrayer.Readings;
        Assert.Equal("Isaiah 58:1-12", mp.First(r => r.Type == ReadingType.FirstLesson).Citation);
        Assert.Equal("Luke 18:9-14", mp.First(r => r.Type == ReadingType.SecondLesson).Citation);
        Assert.Equal("Ps 38", mp.First(r => r.Type == ReadingType.Psalm).Citation);

        var ep = day.DailyOffice.EveningPrayer.Readings;
        Assert.Equal("Jonah 3", ep.First(r => r.Type == ReadingType.FirstLesson).Citation);
        Assert.Equal("1 Corinthians 9:24-27", ep.First(r => r.Type == ReadingType.SecondLesson).Citation);
        Assert.Equal("Ps 6, 32", ep.First(r => r.Type == ReadingType.Psalm).Citation);
    }

    [Fact]
    public void DailyOffice_OrdinaryEastertideWeekday_DoesNotTriggerMovableOverride()
    {
        // Easter 2026 + 10 days = April 15, 2026 — an ordinary Eastertide Wednesday,
        // not one of the 7 discrete movable-Holy-Day offsets. Guards against
        // misreading "through Easter Day" as a date range instead of exact offsets
        // (ADR 0004 already corrected one such misreading of a Daily Office marker).
        var day = _calendar.GetDay(new DateOnly(2026, 4, 15));

        // The regular grid uses MorningPrayer/EveningPrayer reading types, never
        // FirstLesson/SecondLesson — that combination is exclusive to the movable
        // Holy Day override.
        Assert.DoesNotContain(day.DailyOffice.MorningPrayer.Readings, r => r.Type == ReadingType.FirstLesson);
        Assert.Contains(day.DailyOffice.MorningPrayer.Readings, r => r.Type == ReadingType.MorningPrayer);
    }

    // ── Psalm cycle fallback ──────────────────────────────────────────────────

    [Fact]
    public void DailyOffice_Day31_FallsBackTo60DayPsalterCycle()
    {
        // The 30-day cycle has no fixed entry for day 31 (BCP grants free discretion —
        // see issue #30); the loader must fall back to the 60-day cycle so a Psalm
        // reading is never missing.
        var day = _calendar.GetDay(new DateOnly(2025, 1, 31));

        var mpPsalm = day.DailyOffice.MorningPrayer.Readings.First(r => r.Type == ReadingType.Psalm);
        Assert.Equal("Ps 78:1-18", mpPsalm.Citation);
        Assert.Equal("NCP", mpPsalm.TranslationCode);

        var epPsalm = day.DailyOffice.EveningPrayer.Readings.First(r => r.Type == ReadingType.Psalm);
        Assert.Equal("Ps 78:19-40", epPsalm.Citation);
    }

    // ── Non-interference with the Sunday Eucharist lectionary ────────────────

    [Fact]
    public void DailyOffice_AndSundayReadings_BothPopulated_OnASunday()
    {
        // First Sunday of Advent 2025 = Nov 30, 2025 — already covered by
        // LectionaryTests for the Eucharist side; here we confirm DailyOffice is
        // independently populated on the same day without disturbing it.
        var day = _calendar.GetDay(new DateOnly(2025, 11, 30));

        Assert.NotEmpty(day.Readings);
        Assert.NotEmpty(day.DailyOffice.MorningPrayer.Readings);
        Assert.NotEmpty(day.DailyOffice.EveningPrayer.Readings);
    }

    [Fact]
    public void DailyOffice_IsPopulated_OnAnOrdinaryWeekdayWithNoEucharistReadings()
    {
        // Jan 8, 2025 falls in the "Days after Epiphany" gap (WeekNumber 0, between
        // Epiphany Jan 6 and the first Sunday after, Jan 12) — AcnaSundayLectionary
        // deliberately returns no Sunday proper for this stretch, so Readings is
        // empty. The Daily Office is prayed every day regardless of the Eucharist.
        var day = _calendar.GetDay(new DateOnly(2025, 1, 8));

        Assert.Empty(day.Readings);
        Assert.NotEmpty(day.DailyOffice.MorningPrayer.Readings);
        Assert.NotEmpty(day.DailyOffice.EveningPrayer.Readings);
    }
}
