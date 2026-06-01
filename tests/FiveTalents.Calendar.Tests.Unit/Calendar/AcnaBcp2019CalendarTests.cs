using FiveTalents.Calendar.Calendar;
using FiveTalents.Calendar.Feasts;
using FiveTalents.Calendar.Seasons;

namespace FiveTalents.Calendar.Tests.Unit.Calendar;

public sealed class AcnaBcp2019CalendarTests
{
    private readonly AcnaBcp2019Calendar _calendar = new();

    // ── Tradition ─────────────────────────────────────────────────────────────

    [Fact]
    public void Tradition_IsAcnaBcp2019() =>
        Assert.Equal(LiturgicalTradition.AcnaBcp2019, _calendar.Tradition);

    // ── Easter ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(2025, 4, 20)]
    [InlineData(2026, 4, 5)]
    public void GetEaster_ReturnsKnownDate(int year, int month, int day) =>
        Assert.Equal(new DateOnly(year, month, day), _calendar.GetEaster(year));

    // ── Liturgical year ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(2025, 6, 15, 2024)]
    [InlineData(2025, 11, 30, 2025)] // First Sunday of Advent 2025
    [InlineData(2026, 1, 1, 2025)]
    [InlineData(2026, 12, 25, 2026)]
    public void GetLiturgicalYear_ReturnsYearOfGoverningAdvent(int y, int m, int d, int expected) =>
        Assert.Equal(expected, _calendar.GetLiturgicalYear(new DateOnly(y, m, d)));

    // ── Season boundaries — 2026 ──────────────────────────────────────────────
    // Easter Apr 5. Ash Wed Feb 18. Palm Sunday Mar 29. Pentecost May 24. Advent Nov 29.

    [Theory]
    [InlineData(2026, 1, 1, LiturgicalSeason.Christmas)]
    [InlineData(2026, 1, 5, LiturgicalSeason.Christmas)]
    [InlineData(2026, 1, 6, LiturgicalSeason.Epiphany)]
    [InlineData(2026, 2, 17, LiturgicalSeason.Epiphany)]
    [InlineData(2026, 2, 18, LiturgicalSeason.Lent)]
    [InlineData(2026, 3, 28, LiturgicalSeason.Lent)]
    [InlineData(2026, 3, 29, LiturgicalSeason.HolyWeek)]
    [InlineData(2026, 4, 4, LiturgicalSeason.HolyWeek)]
    [InlineData(2026, 4, 5, LiturgicalSeason.Easter)]
    [InlineData(2026, 5, 23, LiturgicalSeason.Easter)]
    [InlineData(2026, 5, 24, LiturgicalSeason.Pentecost)]
    [InlineData(2026, 5, 25, LiturgicalSeason.OrdinaryTime)]
    [InlineData(2026, 11, 28, LiturgicalSeason.OrdinaryTime)]
    [InlineData(2026, 11, 29, LiturgicalSeason.Advent)]
    [InlineData(2026, 12, 24, LiturgicalSeason.Advent)]
    [InlineData(2026, 12, 25, LiturgicalSeason.Christmas)]
    [InlineData(2026, 12, 31, LiturgicalSeason.Christmas)]
    public void GetDay_Season_MatchesExpected(int y, int m, int d, LiturgicalSeason expected) =>
        Assert.Equal(expected, _calendar.GetDay(new DateOnly(y, m, d)).Season);

    // ── Week numbering ────────────────────────────────────────────────────────

    [Theory]
    // Advent 2026 starts Nov 29 (Sunday → week 1)
    [InlineData(2026, 11, 29, LiturgicalSeason.Advent, 1)]
    [InlineData(2026, 12, 3, LiturgicalSeason.Advent, 1)] // Thursday of Advent week 1
    [InlineData(2026, 12, 6, LiturgicalSeason.Advent, 2)]
    [InlineData(2026, 12, 13, LiturgicalSeason.Advent, 3)]
    [InlineData(2026, 12, 20, LiturgicalSeason.Advent, 4)]
    // Christmas 2025: Dec 25 (Thu) = day 1, week 1; Jan 1 = day 8, week 2; Jan 4 (Sun) = week 2
    [InlineData(2025, 12, 25, LiturgicalSeason.Christmas, 1)]
    [InlineData(2025, 12, 31, LiturgicalSeason.Christmas, 1)]
    [InlineData(2026, 1, 1, LiturgicalSeason.Christmas, 2)]
    [InlineData(2026, 1, 4, LiturgicalSeason.Christmas, 2)] // Second Sunday of Christmas
    // Epiphany 2026: Jan 6 (Tue). Week 0 = Jan 6-10. Week 1 starts Jan 11 (First Sunday of Epiphany)
    [InlineData(2026, 1, 6, LiturgicalSeason.Epiphany, 0)] // Epiphany itself — before first Sunday
    [InlineData(2026, 1, 10, LiturgicalSeason.Epiphany, 0)] // last "Day After Epiphany"
    [InlineData(2026, 1, 11, LiturgicalSeason.Epiphany, 1)] // First Sunday of Epiphany
    [InlineData(2026, 1, 18, LiturgicalSeason.Epiphany, 2)] // Second Sunday
    [InlineData(2026, 2, 1, LiturgicalSeason.Epiphany, 4)] // Fourth Sunday of Epiphany
    // Lent 2026: Ash Wed Feb 18 (Wed). Week 0 = Feb 18-21. Week 1 starts Feb 22 (First Sunday of Lent)
    [InlineData(2026, 2, 18, LiturgicalSeason.Lent, 0)] // Ash Wednesday
    [InlineData(2026, 2, 21, LiturgicalSeason.Lent, 0)] // Saturday before First Sunday of Lent
    [InlineData(2026, 2, 22, LiturgicalSeason.Lent, 1)] // First Sunday of Lent
    // Easter 2026 = Apr 5 (Sunday → week 1)
    [InlineData(2026, 4, 5, LiturgicalSeason.Easter, 1)]
    [InlineData(2026, 4, 12, LiturgicalSeason.Easter, 2)]
    [InlineData(2026, 5, 17, LiturgicalSeason.Easter, 7)]
    // Season after Pentecost: Pentecost May 24 (Sun). Week 0 = May 25-30. Week 1 = Trinity Sunday May 31
    [InlineData(2026, 5, 25, LiturgicalSeason.OrdinaryTime, 0)] // Monday after Pentecost
    [InlineData(2026, 5, 30, LiturgicalSeason.OrdinaryTime, 0)] // Saturday before Trinity
    [InlineData(2026, 5, 31, LiturgicalSeason.OrdinaryTime, 1)] // Trinity Sunday
    [InlineData(2026, 6, 7, LiturgicalSeason.OrdinaryTime, 2)]
    public void GetDay_WeekNumber_MatchesExpected(int y, int m, int d, LiturgicalSeason season, int weekNumber)
    {
        var day = _calendar.GetDay(new DateOnly(y, m, d));
        Assert.Equal(season, day.Season);
        Assert.Equal(weekNumber, day.Week.WeekNumber);
    }

    // ── Lectionary year cycling ───────────────────────────────────────────────

    [Theory]
    [InlineData(2023, 12, 3, 'B')]
    [InlineData(2024, 6, 1, 'B')]
    [InlineData(2024, 12, 1, 'C')]
    [InlineData(2025, 4, 20, 'C')]
    [InlineData(2025, 11, 30, 'A')]
    [InlineData(2026, 4, 5, 'A')]
    [InlineData(2026, 11, 29, 'B')]
    public void GetDay_LectionaryYear_CyclesCorrectly(int y, int m, int d, char expected) =>
        Assert.Equal(expected, _calendar.GetDay(new DateOnly(y, m, d)).Week.LectionaryYear);

    // ── Principal feasts and Holy Days ───────────────────────────────────────

    [Theory]
    [InlineData(2026, 2, 18, "Ash Wednesday")]
    [InlineData(2026, 3, 29, "Palm Sunday")]
    [InlineData(2026, 4, 2, "Maundy Thursday")]
    [InlineData(2026, 4, 3, "Good Friday")]
    [InlineData(2026, 4, 5, "Easter Day")]
    [InlineData(2026, 5, 14, "Ascension Day")]
    [InlineData(2026, 5, 24, "The Day of Pentecost")]
    [InlineData(2026, 1, 6, "The Epiphany of Our Lord Jesus Christ")]
    [InlineData(2026, 1, 1, "The Circumcision and Holy Name of Our Lord Jesus Christ")]
    [InlineData(2026, 3, 19, "Joseph, the Guardian of Jesus")]
    [InlineData(2026, 3, 25, "The Annunciation of Our Lord Jesus Christ to the Virgin Mary")]
    [InlineData(2026, 5, 31, "Trinity Sunday")]   // Trinity beats Visitation in 2026
    [InlineData(2026, 8, 6, "The Transfiguration of Our Lord Jesus Christ")]
    [InlineData(2026, 9, 29, "Holy Michael and All Angels")]
    [InlineData(2026, 10, 28, "Simon and Jude, Apostles")]
    [InlineData(2026, 11, 1, "All Saints' Day")]
    [InlineData(2026, 12, 25, "Christmas Day")]
    public void GetDay_Feast_HasExpectedName(int y, int m, int d, string expected)
    {
        var day = _calendar.GetDay(new DateOnly(y, m, d));
        Assert.NotNull(day.Feast);
        Assert.Equal(expected, day.Feast.Name);
    }

    [Fact]
    public void GetDay_Visitation_AppearsInYearWhereNotDisplacedByTrinity()
    {
        // In 2025 Pentecost = June 8, Trinity = June 15, so Visitation (May 31) is free
        var day = _calendar.GetDay(new DateOnly(2025, 5, 31));
        Assert.NotNull(day.Feast);
        Assert.Equal("The Visitation of the Virgin Mary to Elizabeth and Zechariah", day.Feast.Name);
    }

    // ── Feast rank corrections ────────────────────────────────────────────────

    [Theory]
    [InlineData(2026, 3, 25)]  // Annunciation
    [InlineData(2026, 8, 6)]  // Transfiguration
    public void GetDay_FormerlyPrincipalFeasts_AreNowMajor(int y, int m, int d) =>
        Assert.Equal(FeastRank.Major, _calendar.GetDay(new DateOnly(y, m, d)).Feast!.Rank);

    // ── Commemorations ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(2026, 1, 13, "Hilary of Poitiers, Bishop and Teacher of the Faith, 367", FeastRank.Commemoration)]
    [InlineData(2026, 1, 30, "Charles, King and Martyr, 1649", FeastRank.Optional)]
    [InlineData(2026, 3, 17, "Patrick, Bishop and Apostle to the Irish, 461", FeastRank.Optional)]
    [InlineData(2026, 6, 5, "Boniface, Archbishop of Mainz, Missionary to the Germans and Martyr, 754", FeastRank.Optional)]
    [InlineData(2026, 11, 2, "Commemoration of the Faithful Departed (All Souls' Day)", FeastRank.Commemoration)]
    [InlineData(2026, 11, 29, "Clive Staples Lewis, Teacher of the Faith, 1963", FeastRank.Optional)]
    public void GetDay_Commemoration_IsInList(int y, int m, int d, string name, FeastRank rank)
    {
        var day = _calendar.GetDay(new DateOnly(y, m, d));
        var match = day.Commemorations.FirstOrDefault(c => c.Name == name);
        Assert.NotNull(match);
        Assert.Equal(rank, match.Rank);
    }

    [Fact]
    public void GetDay_Commemoration_HasNullColor()
    {
        var day = _calendar.GetDay(new DateOnly(2026, 1, 13)); // Hilary of Poitiers
        var hilary = day.Commemorations.First(c => c.Name.StartsWith("Hilary"));
        Assert.Null(hilary.Color);
    }

    // ── Rogation Days ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(2026, 5, 11)] // Mon before Ascension (May 14)
    [InlineData(2026, 5, 12)] // Tue
    [InlineData(2026, 5, 13)] // Wed
    public void GetDay_RogationDays_AreMarked(int y, int m, int d) =>
        Assert.True(_calendar.GetDay(new DateOnly(y, m, d)).IsRogationDay);

    [Fact]
    public void GetDay_AscensionDay_IsNotRogationDay() =>
        Assert.False(_calendar.GetDay(new DateOnly(2026, 5, 14)).IsRogationDay);

    // ── Ember Days ───────────────────────────────────────────────────────────

    [Theory]
    // After First Sunday of Lent (Feb 22, 2026) → Wed Feb 25, Fri Feb 27, Sat Feb 28
    [InlineData(2026, 2, 25)]
    [InlineData(2026, 2, 27)]
    [InlineData(2026, 2, 28)]
    // After Pentecost (May 24, 2026) → Wed May 27, Fri May 29, Sat May 30
    [InlineData(2026, 5, 27)]
    [InlineData(2026, 5, 29)]
    [InlineData(2026, 5, 30)]
    // After Holy Cross Day (Sep 14, 2026, Monday) → Wed Sep 16, Fri Sep 18, Sat Sep 19
    [InlineData(2026, 9, 16)]
    [InlineData(2026, 9, 18)]
    [InlineData(2026, 9, 19)]
    // After St. Lucy's Day (Dec 13, 2026, Sunday) → Wed Dec 16, Fri Dec 18, Sat Dec 19
    [InlineData(2026, 12, 16)]
    [InlineData(2026, 12, 18)]
    [InlineData(2026, 12, 19)]
    public void GetDay_EmberDays_AreMarked(int y, int m, int d) =>
        Assert.True(_calendar.GetDay(new DateOnly(y, m, d)).IsEmberDay);

    [Fact]
    public void GetDay_ThursdayInEmberWeek_IsNotEmberDay() =>
        Assert.False(_calendar.GetDay(new DateOnly(2026, 2, 26)).IsEmberDay);

    // ── Fast Days ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(2026, 2, 19)] // Thursday after Ash Wednesday (Lent weekday)
    [InlineData(2026, 3, 13)] // Friday in Lent
    [InlineData(2026, 4, 1)] // Wednesday of Holy Week
    [InlineData(2026, 6, 26)] // Friday in OrdinaryTime
    [InlineData(2026, 11, 13)] // Friday in Advent
    public void GetDay_FastDays_AreMarked(int y, int m, int d) =>
        Assert.True(_calendar.GetDay(new DateOnly(y, m, d)).IsFastDay);

    [Theory]
    [InlineData(2026, 4, 5)] // Easter Sunday — not a fast day
    [InlineData(2026, 4, 10)] // Friday in Eastertide
    [InlineData(2026, 1, 2)] // Friday in Christmas season
    [InlineData(2026, 2, 22)] // First Sunday of Lent — Sundays are never fast days
    public void GetDay_NonFastDays_AreNotMarked(int y, int m, int d) =>
        Assert.False(_calendar.GetDay(new DateOnly(y, m, d)).IsFastDay);

    // ── GetRange ──────────────────────────────────────────────────────────────

    [Fact]
    public void GetRange_ReturnsContiguousDays()
    {
        DateOnly from = new DateOnly(2026, 4, 1);
        DateOnly to = new DateOnly(2026, 4, 30);
        var days = _calendar.GetRange(from, to);
        Assert.Equal(30, days.Count);
        for (int i = 0; i < days.Count - 1; i++)
        {
            Assert.Equal(days[i].Date.AddDays(1), days[i + 1].Date);
        }
    }

    [Fact]
    public void GetRange_ThrowsWhenToBeforeFrom() =>
        Assert.Throws<ArgumentException>(() =>
            _calendar.GetRange(new DateOnly(2026, 4, 30), new DateOnly(2026, 4, 1)));
}
