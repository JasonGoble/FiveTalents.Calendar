using FiveTalents.Calendar.Calendar;

namespace FiveTalents.Calendar.Tests.Unit.Calendar;

/// <summary>
/// Verifies <see cref="LiturgicalDay.Occurrences"/> — the ordered occurrence stack that
/// replaced the old <c>Feast</c>/<c>Commemorations</c>/<c>SundayTitle</c> split. See ADR 0016.
/// </summary>
public sealed class OccurrenceStackTests
{
    private readonly AcnaBcp2019Calendar _calendar = new();

    // ── The issue's own worked example: three simultaneous occurrences ───────────

    [Fact]
    public void GetDay_Dec27_2026_HasThreeOccurrencesInOrder()
    {
        // Sunday; John the Apostle (Major Feast, fixed Dec 27) collides with the First
        // Sunday of Christmas, which is also the Third Day of Christmas.
        var day = _calendar.GetDay(new DateOnly(2026, 12, 27));

        Assert.Equal(3, day.Occurrences.Count);

        var feast = day.Occurrences[0];
        Assert.Equal(OccurrenceType.MajorFeast, feast.Type);
        Assert.Equal("John, Apostle and Evangelist", feast.Name);
        Assert.Equal(ObservancePrecedence.Prescribed, feast.Precedence);
        Assert.NotEmpty(feast.Services);

        var sunday = day.Occurrences[1];
        Assert.Equal(OccurrenceType.Sunday, sunday.Type);
        Assert.Equal("The First Sunday of Christmas", sunday.Name);

        var seasonDay = day.Occurrences[2];
        Assert.Equal(OccurrenceType.SeasonDay, seasonDay.Type);
        Assert.Equal("The Third Day of Christmas", seasonDay.Name);
        Assert.Equal(ObservancePrecedence.Supplementary, seasonDay.Precedence);

        // Readings still resolve to the highest-precedence occurrence's own services.
        Assert.Equal(feast.Services.SelectMany(s => s.Readings).Select(r => r.Citation),
            day.Readings.SelectMany(s => s.Readings).Select(r => r.Citation));
    }

    // ── Rogation Day: one occurrence, consistent with the IsRogationDay flag ─────

    [Theory]
    [InlineData(2026, 5, 11)]
    [InlineData(2026, 5, 12)]
    [InlineData(2026, 5, 13)]
    public void GetDay_RogationDay_HasExactlyOneRogationDayOccurrence(int y, int m, int d)
    {
        var day = _calendar.GetDay(new DateOnly(y, m, d));

        Assert.True(day.IsRogationDay);
        List<Occurrence> rogationOccurrences = day.Occurrences.Where(o => o.Type == OccurrenceType.RogationDay).ToList();
        Assert.Single(rogationOccurrences);
        Assert.Equal(ObservancePrecedence.Supplementary, rogationOccurrences[0].Precedence);
    }

    [Fact]
    public void GetDay_AscensionDay_HasNoRogationDayOccurrence()
    {
        var day = _calendar.GetDay(new DateOnly(2026, 5, 14));

        Assert.False(day.IsRogationDay);
        Assert.DoesNotContain(day.Occurrences, o => o.Type == OccurrenceType.RogationDay);
    }

    // ── Ember Day: a bare placeholder occurrence, no crash ───────────────────────

    [Fact]
    public void GetDay_EmberDay_HasBareEmberDayOccurrence()
    {
        // After First Sunday of Lent (Feb 22, 2026) → Wed Feb 25
        var day = _calendar.GetDay(new DateOnly(2026, 2, 25));

        Assert.True(day.IsEmberDay);
        var emberOccurrence = Assert.Single(day.Occurrences, o => o.Type == OccurrenceType.EmberDay);
        Assert.Null(emberOccurrence.Name);
        Assert.Empty(emberOccurrence.Services);
        Assert.Equal(ObservancePrecedence.Supplementary, emberOccurrence.Precedence);
    }

    // ── Antiphon: no content source exists yet, so no date should ever produce one ────

    [Fact]
    public void GetRange_FullYear_NeverProducesAnAntiphonOccurrence()
    {
        var days = _calendar.GetRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        Assert.DoesNotContain(days, day => day.Occurrences.Any(o => o.Type == OccurrenceType.Antiphon));
    }

    // ── General Sunday naming — every Sunday gets a name now, not just the three ────
    // ── BCP-specific special cases (covered in AcnaBcp2019CalendarTests) ────────

    [Theory]
    [InlineData(2026, 11, 29, "The First Sunday of Advent")]
    [InlineData(2026, 2, 22, "The First Sunday of Lent")]
    [InlineData(2026, 4, 12, "The Second Sunday of Easter")]
    public void GetDay_OrdinarySunday_GetsAGeneralName(int y, int m, int d, string expected)
    {
        var day = _calendar.GetDay(new DateOnly(y, m, d));

        var sunday = Assert.Single(day.Occurrences, o => o.Type == OccurrenceType.Sunday);
        Assert.Equal(expected, sunday.Name);
    }

    // ── No duplicate Sunday entry when a Feast already owns its own Sunday slot ──────
    // ADR 0008: when the Feast's lectionary key equals the season's, the single Feast
    // entry already is that Sunday's identity — no separate Sunday-type occurrence.

    [Theory]
    [InlineData(2026, 5, 31)] // Trinity Sunday
    [InlineData(2026, 4, 5)]  // Easter Day
    [InlineData(2026, 3, 29)] // Palm Sunday
    public void GetDay_FeastOwnsItsOwnSunday_HasNoSeparateSundayOccurrence(int y, int m, int d)
    {
        var day = _calendar.GetDay(new DateOnly(y, m, d));

        Assert.DoesNotContain(day.Occurrences, o => o.Type == OccurrenceType.Sunday);
    }
}
