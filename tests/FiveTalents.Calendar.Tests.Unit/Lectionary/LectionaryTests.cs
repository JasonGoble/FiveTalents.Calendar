using FiveTalents.Calendar.Calendar;
using FiveTalents.Calendar.Feasts;
using FiveTalents.Calendar.Lectionary;
using FiveTalents.Calendar.Seasons;

namespace FiveTalents.Calendar.Tests.Unit.Lectionary;

/// <summary>
/// Verifies that the Sunday lectionary JSON loads correctly and returns the
/// expected readings. All citations cross-checked against BCP 2019 document
/// #58 (Sunday, Holy Day and Commemoration Lectionary).
/// </summary>
public sealed class LectionaryTests
{
    private readonly AcnaBcp2019Calendar _calendar = new();

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Readings from the primary (highest-priority) observance.</summary>
    private static IReadOnlyList<LectionaryReading> PrimaryReadings(LiturgicalDay day) =>
        day.Observances.First().ReadingOptions[0].Readings;

    /// <summary>ReadingOptions from the named observance.</summary>
    private static IReadOnlyList<ReadingSet> ObservanceReadings(LiturgicalDay day, string name) =>
        day.Observances.First(o => o.Name == name).ReadingOptions;

    // ── ProperNumber ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(2026, 5, 25, 3)]
    [InlineData(2026, 5, 31, 4)]
    [InlineData(2026, 6, 7, 5)]
    [InlineData(2026, 6, 8, 5)]
    [InlineData(2026, 11, 1, 26)]
    [InlineData(2026, 11, 22, 29)]
    [InlineData(2026, 2, 18, null)]
    [InlineData(2026, 4, 5, null)]
    public void GetDay_ProperNumber_IsCorrect(int y, int m, int d, int? expected)
    {
        var day = _calendar.GetDay(new DateOnly(y, m, d));
        Assert.Equal(expected, day.ProperNumber);
    }

    // ── Advent readings ───────────────────────────────────────────────────────

    [Fact]
    public void Readings_Advent1_YearA_ReturnsCorrectCitations()
    {
        // First Sunday of Advent 2025 = Nov 30, 2025 (Year A)
        var day = _calendar.GetDay(new DateOnly(2025, 11, 30));
        Assert.Equal('A', day.Week.LectionaryYear);
        Assert.Equal(LiturgicalSeason.Advent, day.Season);

        var readings = PrimaryReadings(day);
        Assert.Equal("Isa 2:1-5", readings.First(r => r.Type == ReadingType.OldTestament).Citation);
        Assert.Equal("Matt 24:29-44", readings.First(r => r.Type == ReadingType.Gospel).Citation);
    }

    [Fact]
    public void Readings_Advent1_YearC_HasAlternateCitations()
    {
        // First Sunday of Advent 2024 = Dec 1, 2024 (Year C)
        var day = _calendar.GetDay(new DateOnly(2024, 12, 1));
        Assert.Equal('C', day.Week.LectionaryYear);

        var readings = PrimaryReadings(day);
        var ot = readings.First(r => r.Type == ReadingType.OldTestament);
        Assert.Equal("Zech 14:1-9", ot.Citation);
        Assert.Equal("Zech 14:3-9", ot.AlternateCitation);

        var psalm = readings.First(r => r.Type == ReadingType.Psalm);
        Assert.Equal("Ps 50", psalm.Citation);
        Assert.Equal("Ps 50:1-6", psalm.AlternateCitation);
    }

    // ── Christmas readings ────────────────────────────────────────────────────

    [Fact]
    public void Readings_ChristmasDay_ReturnsThreeLabeledSets()
    {
        var day = _calendar.GetDay(new DateOnly(2026, 12, 25));
        var christmasObs = day.Observances.First(o => o.Name == "Christmas Day");

        Assert.Equal(3, christmasObs.ReadingOptions.Count);
        Assert.Equal("I", christmasObs.ReadingOptions[0].Label);
        Assert.Equal("II", christmasObs.ReadingOptions[1].Label);
        Assert.Equal("III", christmasObs.ReadingOptions[2].Label);

        // Set I — Isa 9 / Ps 96 / Titus 2 / Luke 2:1-20
        var setI = christmasObs.ReadingOptions[0].Readings;
        Assert.Equal("Isa 9:1-7", setI.First(r => r.Type == ReadingType.OldTestament).Citation);
        var gospelI = setI.First(r => r.Type == ReadingType.Gospel);
        Assert.Equal("Luke 2:1-20", gospelI.Citation);
        Assert.Equal("Luke 2:1-14", gospelI.AlternateCitation);

        // Set III — John 1:1-18
        var setIII = christmasObs.ReadingOptions[2].Readings;
        Assert.Equal("John 1:1-18", setIII.First(r => r.Type == ReadingType.Gospel).Citation);
    }

    [Fact]
    public void Readings_CircumcisionHolyName_ReturnsCorrectReadings()
    {
        // Jan 1: feast "The Circumcision and Holy Name..." maps to CircumcisionHolyName
        var day = _calendar.GetDay(new DateOnly(2026, 1, 1));
        var readings = PrimaryReadings(day);
        Assert.Equal("Ex 34:1-9", readings.First(r => r.Type == ReadingType.OldTestament).Citation);
        Assert.Equal("Luke 2:15-21", readings.First(r => r.Type == ReadingType.Gospel).Citation);
    }

    // ── Epiphany readings ─────────────────────────────────────────────────────

    [Fact]
    public void Readings_Epiphany1_BaptismOfOurLord_YearA()
    {
        // First Sunday of Epiphany 2026 = Jan 11, 2026 (Year A)
        var day = _calendar.GetDay(new DateOnly(2026, 1, 11));
        Assert.Equal(LiturgicalSeason.Epiphany, day.Season);
        Assert.Equal(1, day.Week.WeekNumber);

        var readings = PrimaryReadings(day);
        Assert.Equal("Matt 3:13-17", readings.First(r => r.Type == ReadingType.Gospel).Citation);
        Assert.Equal("Acts 10:34-38", readings.First(r => r.Type == ReadingType.Epistle).Citation);
    }

    [Fact]
    public void Readings_EpiphanyDaysAfter_WeekZero_HaveNoSeasonProper()
    {
        // Jan 7, 2026 = "Day After Epiphany" (week 0) — no Sunday proper
        var day = _calendar.GetDay(new DateOnly(2026, 1, 7));
        Assert.Equal(LiturgicalSeason.Epiphany, day.Season);
        Assert.Equal(0, day.Week.WeekNumber);
        // No observances with readings on a weekday in week 0
        Assert.DoesNotContain(day.Observances, o => o.ReadingOptions.Count > 0);
    }

    // ── Lent readings ─────────────────────────────────────────────────────────

    [Fact]
    public void Readings_AshWednesday_HasOrAlternative()
    {
        var day = _calendar.GetDay(new DateOnly(2026, 2, 18));
        Assert.Equal("Ash Wednesday", day.Observances.First().Name);

        var ot = PrimaryReadings(day).First(r => r.Type == ReadingType.OldTestament);
        Assert.Equal("Joel 2:1-2,12-17", ot.Citation);
        Assert.Equal("Isa 58:1-12", ot.AlternateCitation);
    }

    [Fact]
    public void Readings_Lent2_YearA_RomParenthetical()
    {
        // Second Sunday of Lent 2026 = March 1 (Year A)
        var day = _calendar.GetDay(new DateOnly(2026, 3, 1));
        Assert.Equal(LiturgicalSeason.Lent, day.Season);
        Assert.Equal(2, day.Week.WeekNumber);

        var epistle = PrimaryReadings(day).First(r => r.Type == ReadingType.Epistle);
        Assert.Equal("Rom 4:1-17", epistle.Citation);
        Assert.Equal("Rom 4:1-5,13-17", epistle.AlternateCitation);
    }

    // ── Holy Week readings ────────────────────────────────────────────────────

    [Fact]
    public void Readings_GoodFriday_HasReadings()
    {
        var day = _calendar.GetDay(new DateOnly(2026, 4, 3));
        Assert.Equal("Good Friday", day.Observances.First().Name);

        var readings = PrimaryReadings(day);
        Assert.Equal(4, readings.Count);
        var gospel = readings.First(r => r.Type == ReadingType.Gospel);
        Assert.Equal("John 18:1-19:37", gospel.Citation);
        Assert.Equal("John 19:1-37", gospel.AlternateCitation);
    }

    // ── Easter readings ───────────────────────────────────────────────────────

    [Fact]
    public void Readings_EasterDay_YearA_HasFourReadings()
    {
        var day = _calendar.GetDay(new DateOnly(2026, 4, 5));
        Assert.Equal("Easter Day", day.Observances.First().Name);

        var readings = PrimaryReadings(day);
        Assert.Equal(4, readings.Count);
        Assert.Equal("John 20:1-18", readings.First(r => r.Type == ReadingType.Gospel).Citation);
    }

    [Fact]
    public void Readings_EasterMonday_HasReadings()
    {
        var day = _calendar.GetDay(new DateOnly(2026, 4, 6));
        Assert.Equal(LiturgicalSeason.Easter, day.Season);
        Assert.NotEmpty(day.Observances);
        Assert.Equal("Matt 28:9-15", PrimaryReadings(day).First(r => r.Type == ReadingType.Gospel).Citation);
    }

    // ── Season after Pentecost readings ───────────────────────────────────────

    [Fact]
    public void Readings_TrinitySunday_YearA_HasCorrectReadings()
    {
        // Trinity Sunday 2026 = May 31 (Year A)
        var day = _calendar.GetDay(new DateOnly(2026, 5, 31));
        Assert.Equal("Trinity Sunday", day.Observances.First().Name);
        Assert.Equal("Matt 28:16-20", PrimaryReadings(day).First(r => r.Type == ReadingType.Gospel).Citation);
    }

    [Fact]
    public void Readings_Proper5_YearA_IsCorrect()
    {
        // June 7, 2026 = first Sunday after Trinity (Proper 5, Year A)
        var day = _calendar.GetDay(new DateOnly(2026, 6, 7));
        Assert.Equal(5, day.ProperNumber);
        Assert.Equal('A', day.Week.LectionaryYear);

        var readings = PrimaryReadings(day);
        Assert.Equal("Hos 5:15-6:6", readings.First(r => r.Type == ReadingType.OldTestament).Citation);
        Assert.Equal("Matt 9:9-13", readings.First(r => r.Type == ReadingType.Gospel).Citation);
    }

    [Fact]
    public void Readings_Proper29_ChristTheKing_YearA()
    {
        // Nov 22, 2026 = Proper 29 (Christ the King), Year A
        var day = _calendar.GetDay(new DateOnly(2026, 11, 22));
        Assert.Equal(29, day.ProperNumber);
        Assert.Equal('A', day.Week.LectionaryYear);
        Assert.Equal("Matt 25:31-46", PrimaryReadings(day).First(r => r.Type == ReadingType.Gospel).Citation);
    }

    // ── Holy Day readings ─────────────────────────────────────────────────────

    [Fact]
    public void Readings_AllSaints_HasOtAndAlternate()
    {
        var day = _calendar.GetDay(new DateOnly(2026, 11, 1));
        Assert.Equal("All Saints' Day", day.Observances.First().Name);

        var ot = PrimaryReadings(day).First(r => r.Type == ReadingType.OldTestament);
        Assert.Equal("Ecclesiasticus 44:1-14", ot.Citation);
        Assert.Equal("Rev 7:9-17", ot.AlternateCitation);
    }

    [Fact]
    public void Readings_Andrew_ReturnsHolyDayReadings()
    {
        // Nov 30, 2026 = Monday of Advent + St. Andrew's Day
        var day = _calendar.GetDay(new DateOnly(2026, 11, 30));
        Assert.Equal("Andrew the Apostle", day.Observances.First().Name);
        Assert.Equal("Matt 4:18-22", PrimaryReadings(day).First(r => r.Type == ReadingType.Gospel).Citation);
    }

    [Fact]
    public void Readings_Annunciation_HasMagnificatAsAlternate()
    {
        var day = _calendar.GetDay(new DateOnly(2026, 3, 25));
        Assert.Equal("The Annunciation of Our Lord Jesus Christ to the Virgin Mary", day.Observances.First().Name);

        var psalm = PrimaryReadings(day).First(r => r.Type == ReadingType.Psalm);
        Assert.Equal("Ps 40:1-13", psalm.Citation);
        Assert.Equal("Magnificat", psalm.AlternateCitation);
    }

    // ── Season proper observance ──────────────────────────────────────────────

    [Fact]
    public void Readings_SundayProper_AppearsAlongsideHolyDay()
    {
        // Oct 18, 2026 = Luke the Evangelist (Major) + season Sunday proper
        var day = _calendar.GetDay(new DateOnly(2026, 10, 18));
        Assert.Equal("Luke the Evangelist and Companion of Paul", day.Observances.First().Name);
        var proper = day.Observances.FirstOrDefault(o => o.Name.StartsWith("Proper"));
        Assert.NotNull(proper);
        Assert.NotEmpty(proper.ReadingOptions);
    }

    [Fact]
    public void Readings_TrinitySunday_VisitationHasOwnReadings()
    {
        // May 31, 2026: Trinity Sunday + The Visitation — both have readings
        var day = _calendar.GetDay(new DateOnly(2026, 5, 31));
        var visitation = day.Observances.First(o => o.Name == "The Visitation of the Virgin Mary to Elizabeth and Zechariah");
        Assert.NotEmpty(visitation.ReadingOptions);
        Assert.NotEmpty(visitation.ReadingOptions[0].Readings);
    }

    // ── Commemoration readings from Common of Saints ──────────────────────────

    [Fact]
    public void Readings_Commemoration_HasCommonReadings()
    {
        // Jan 30, 2026 = Charles, King and Martyr → Common_Martyr readings
        var day = _calendar.GetDay(new DateOnly(2026, 1, 30));
        var charles = day.Observances.First(o => o.Name.StartsWith("Charles, King"));
        Assert.NotEmpty(charles.ReadingOptions);
        Assert.NotEmpty(charles.ReadingOptions[0].Readings);
    }

    // ── Ordinary weekdays ─────────────────────────────────────────────────────

    [Fact]
    public void Readings_OrdinaryWeekday_HasNoSeasonProperObservance()
    {
        // Tuesday July 7, 2026: no feast, OrdinaryTime — season proper is Sundays only
        var day = _calendar.GetDay(new DateOnly(2026, 7, 7));
        Assert.Equal(LiturgicalSeason.OrdinaryTime, day.Season);
        Assert.Equal(9, day.ProperNumber);
        Assert.DoesNotContain(day.Observances, o => o.Rank >= FeastRank.Major);
    }
}
