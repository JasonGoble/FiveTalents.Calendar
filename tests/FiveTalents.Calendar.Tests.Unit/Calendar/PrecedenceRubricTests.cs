using FiveTalents.Calendar.Calendar;
using FiveTalents.Calendar.Feasts;
using FiveTalents.Calendar.Lectionary;
using FiveTalents.Calendar.Seasons;

namespace FiveTalents.Calendar.Tests.Unit.Calendar;

/// <summary>
/// Targeted tests for the liturgical "unwritten rules" — the precedence and
/// substitution rubrics governing which feast and reading wins when observances
/// collide, as distinct from whether reading text is correctly transcribed (see
/// <c>LectionaryTests</c> and the #18/#19/#20 data-fidelity audits). Cross-referenced
/// against BCP 2019 document #57 (Calendar of the Christian Year), pp. 688–689 —
/// not the lectionary documents. Closes #21.
/// </summary>
public sealed class PrecedenceRubricTests
{
    private readonly AcnaBcp2019Calendar _calendar = new();

    // ── Rubric: Principal Feasts take precedence over any other day or observance ──
    // BCP 2019 p.688: "These feasts take precedence over any other day or observance."

    [Fact]
    public void PrincipalFeast_BeatsCollidingMajorHolyDay_TrinityOverVisitation()
    {
        // 2026: Trinity Sunday (Principal) falls May 31, the fixed date of the
        // Visitation of the Virgin Mary (Major).
        var day = _calendar.GetDay(new DateOnly(2026, 5, 31));
        Assert.Equal("Trinity Sunday", day.Feast!.Name);
        Assert.Equal(FeastRank.Principal, day.Feast.Rank);
    }

    [Fact]
    public void PrincipalFeast_BeatsCollidingMajorHolyDay_EasterDayOverAnnunciation()
    {
        // 2035: Easter Day (Principal) falls on March 25, the fixed date of the
        // Annunciation (Major). Confirms the rank rule holds even when the colliding
        // day is itself a moveable Principal Feast, not just a fixed Holy Day.
        var day = _calendar.GetDay(new DateOnly(2035, 3, 25));
        Assert.Equal("Easter Day", day.Feast!.Name);

        var readings = day.Readings.Single().Readings;
        Assert.Equal("Acts 10:34-43", readings.First(r => r.Type == ReadingType.FirstLesson).Citation);
    }

    // ── Rubric: no holy day may replace the fixed propers of Holy Week or Easter Week ──
    // BCP 2019 p.689: "No holy day or observance can replace the fixed propers for Ash
    // Wednesday, Holy Week, or Easter Week." Monday–Wednesday of Holy Week share
    // FeastRank.Major with most fixed Holy Days, so a naive rank comparison ties —
    // AcnaFeastCatalog.GetHolyDays suppresses fixed Holy Days outright for the whole
    // Palm-Sunday-through-Easter-Saturday range rather than relying on that tie-break.

    [Theory]
    [InlineData(2024, 3, 25, "Monday of Holy Week", "Isa 42:1-9")]
    [InlineData(2043, 3, 25, "Wednesday of Holy Week", "Isa 50:4-9")]
    public void FixedHolyDay_CannotDisplaceHolyWeekWeekdayPropers(
        int y, int m, int d, string expectedFeast, string expectedFirstLesson)
    {
        // Each date is the fixed date of the Annunciation, colliding with a weekday of
        // Holy Week that carries its own fixed propers.
        var day = _calendar.GetDay(new DateOnly(y, m, d));
        Assert.Equal(expectedFeast, day.Feast!.Name);

        var readings = day.Readings.Single().Readings;
        Assert.Equal(expectedFirstLesson, readings.First(r => r.Type == ReadingType.FirstLesson).Citation);
    }

    [Fact]
    public void FixedHolyDay_CannotDisplaceEasterWeekWeekdayPropers()
    {
        // 2160: the Annunciation (March 25) falls on the Tuesday of Easter Week.
        // Easter Week's weekdays have no FeastDay catalog entry at all to rank against
        // — unlike Holy Week's named weekdays — so the fixed feast must be suppressed
        // outright rather than relying on a rank comparison that never happens.
        var day = _calendar.GetDay(new DateOnly(2160, 3, 25));
        Assert.Null(day.Feast);

        var readings = day.Readings.Single().Readings;
        Assert.Equal("Acts 2:14,36-41", readings.First(r => r.Type == ReadingType.FirstLesson).Citation);
    }

    [Theory]
    [InlineData(2029, 3, 25, "Palm Sunday")] // Annunciation coincides with Palm Sunday itself
    [InlineData(2035, 3, 25, "Easter Day")]  // Annunciation coincides with Easter Day itself
    public void FixedHolyDay_StillLosesAtProtectedWeekBoundaryDays(int y, int m, int d, string expectedFeast)
    {
        // Palm Sunday and Easter Day are themselves Principal-ranked moveable feasts,
        // so this already held via rank alone — asserted to confirm the p.689
        // suppression doesn't change behavior at the edges of the protected range.
        var day = _calendar.GetDay(new DateOnly(y, m, d));
        Assert.Equal(expectedFeast, day.Feast!.Name);
    }

    // ── Rubric: a Holy Day on a Sunday, other than in Advent/Lent/Easter, may be
    //    observed on that Sunday ──
    // BCP 2019 p.689: "Any of these feasts that fall on a Sunday, other than in Advent,
    // Lent, and Easter, may be observed on that Sunday or transferred to the nearest
    // following weekday." The engine's default, absent a way to express local pastoral
    // discretion, is to observe on the Sunday.

    [Fact]
    public void HolyDayOnOrdinarySunday_IsObservedOnThatSunday()
    {
        // 2026-10-18: Luke the Evangelist (Major) falls on a Sunday in OrdinaryTime.
        var day = _calendar.GetDay(new DateOnly(2026, 10, 18));
        Assert.Equal(LiturgicalSeason.OrdinaryTime, day.Season);
        Assert.Equal("Luke the Evangelist and Companion of Paul", day.Feast!.Name);

        var readings = day.Readings.Single().Readings;
        Assert.Equal("Ecclesiasticus 38:1-14", readings.First(r => r.Type == ReadingType.FirstLesson).Citation);
    }

    // ── Rubric: a Holy Day on a Sunday of Advent, Lent, or Easter yields to the Sunday
    //    propers ──
    // BCP 2019 p.689: "Any of these feasts that fall on a Sunday, other than in Advent,
    // Lent, and Easter, may be observed on that Sunday or transferred to the nearest
    // following weekday" — read together with the Principal-Feasts-always-win rule
    // (p.688), a non-Principal Holy Day colliding with such a Sunday yields entirely; the
    // Sunday's own readings already reflected this pre-#43, but Feast still (wrongly)
    // reported the displaced Holy Day. Fixed here. See ADR 0006/0007. Closes #43.
    //
    // Deliberately NOT covered: where — or whether — the yielded Holy Day is observed
    // instead. "May be transferred" is pastoral discretion, not something GetDay should
    // decide unilaterally; see issue #30 (discretionary-rubric representation), which
    // this question has been folded into rather than answered here.

    [Theory]
    [InlineData(2025, 11, 30, "Isa 2:1-5")]      // Advent Sunday 1, collides with Andrew the Apostle
    [InlineData(2023, 3, 19, "1 Sam 16:1-13")]   // Lent Sunday 4, collides with Joseph, the Guardian of Jesus
    [InlineData(2021, 4, 25, "Acts 4:23-37")]    // Easter Sunday 4 (not Easter Day itself), collides with Mark the Evangelist
    public void HolyDayOnAdventLentOrEasterSunday_YieldsFeastAndReadingsToSunday(
        int y, int m, int d, string expectedFirstLesson)
    {
        var day = _calendar.GetDay(new DateOnly(y, m, d));

        Assert.Null(day.Feast);

        var readings = day.Readings.Single().Readings;
        Assert.Equal(expectedFirstLesson, readings.First(r => r.Type == ReadingType.FirstLesson).Citation);
    }

    // ── Rubric: weekdays of the Season after Pentecost borrow their governing Sunday's
    //    Proper ──
    // Not a p.57 precedence rule per se, but the class of "unwritten rule" issue #21
    // calls out explicitly. See SeasonResolver.GetProperNumber and ADR 0002.

    [Fact]
    public void OrdinaryTimeWeekday_BorrowsGoverningSundaysReadings()
    {
        var sunday = _calendar.GetDay(new DateOnly(2026, 6, 7)); // Proper 5 Sunday
        var monday = _calendar.GetDay(new DateOnly(2026, 6, 8)); // Monday of the same Proper week

        Assert.Equal(sunday.ProperNumber, monday.ProperNumber);

        string sundayFirstLesson = sunday.Readings.Single().Readings.First(r => r.Type == ReadingType.FirstLesson).Citation;
        string mondayFirstLesson = monday.Readings.Single().Readings.First(r => r.Type == ReadingType.FirstLesson).Citation;
        Assert.Equal(sundayFirstLesson, mondayFirstLesson);
    }
}
