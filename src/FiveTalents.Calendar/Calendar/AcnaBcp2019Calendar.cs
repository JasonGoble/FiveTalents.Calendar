using FiveTalents.Calendar.Feasts;
using FiveTalents.Calendar.Lectionary;
using FiveTalents.Calendar.Seasons;

namespace FiveTalents.Calendar.Calendar;

/// <summary>
/// Liturgical calendar implementation for the Anglican Church in North America,
/// Book of Common Prayer 2019.
/// </summary>
public sealed class AcnaBcp2019Calendar : ILiturgicalCalendar
{
    public LiturgicalTradition Tradition => LiturgicalTradition.AcnaBcp2019;

    public DateOnly GetEaster(int year) => EasterCalculator.GetEaster(year);

    public int GetLiturgicalYear(DateOnly date)
    {
        var adventThisYear = SeasonResolver.GetAdventSunday(date.Year);
        return date >= adventThisYear ? date.Year : date.Year - 1;
    }

    public LiturgicalDay GetDay(DateOnly date)
    {
        var info = SeasonResolver.Resolve(date, date.Year);
        var holyDays = AcnaFeastCatalog.GetHolyDays(date, date.Year);
        var commemorations = AcnaFeastCatalog.GetCommemorations(date, date.Year);

        var primaryFeast = ResolveFeast(date, info.Season, holyDays);

        int? properNumber = SeasonResolver.GetProperNumber(date, info.Season);

        LiturgicalDay day = new LiturgicalDay
        {
            Date = date,
            Season = info.Season,
            Week = new LiturgicalWeek
            {
                Season = info.Season,
                WeekNumber = info.WeekNumber,
                LectionaryYear = info.LectionaryYear,
            },
            Feast = primaryFeast,
            Commemorations = commemorations,
            IsEmberDay = AcnaFeastCatalog.IsEmberDay(date, date.Year),
            IsRogationDay = IsRogationDay(date, date.Year),
            IsFastDay = IsFastDay(date, info.Season),
            ProperNumber = properNumber,
            SundayTitle = GetSundayTitle(date, info.Season, info.WeekNumber, properNumber),
            DailyOffice = AcnaDailyOfficeLectionary.GetReadings(date),
        };

        return day with { Readings = AcnaSundayLectionary.GetReadings(day) };
    }

    public IReadOnlyList<LiturgicalDay> GetRange(DateOnly from, DateOnly to)
    {
        if (to < from)
        {
            throw new ArgumentException("'to' must be on or after 'from'.", nameof(to));
        }

        List<LiturgicalDay> days = new List<LiturgicalDay>();
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            days.Add(GetDay(d));
        }

        return days;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Resolves the primary Feast for <paramref name="date"/>, applying the BCP 2019
    /// p.689 rule that a non-Principal Holy Day falling on a Sunday of Advent, Lent, or
    /// Easter yields to that Sunday's own propers (matching what <see cref="AcnaSundayLectionary"/>
    /// already does for <c>Readings</c>). Where — or whether — a yielded Holy Day is
    /// observed elsewhere is left deliberately unresolved: the rubric only says it "may"
    /// be transferred, which is a pastoral choice, not something this engine should decide
    /// unilaterally. See issue #30 (discretionary-rubric representation) and ADR 0006.
    /// </summary>
    private static FeastDay? ResolveFeast(DateOnly date, LiturgicalSeason season, IReadOnlyList<FeastDay> holyDays)
    {
        var primary = holyDays.Count > 0 ? holyDays.MaxBy(f => (int)f.Rank) : null;

        bool yieldsToSunday = date.DayOfWeek == DayOfWeek.Sunday
            && season is LiturgicalSeason.Advent or LiturgicalSeason.Lent or LiturgicalSeason.Easter
            && primary is not null
            && primary.Rank != FeastRank.Principal;

        return yieldsToSunday ? null : primary;
    }

    private static bool IsRogationDay(DateOnly date, int year)
    {
        var ascension = EasterCalculator.GetEaster(year).AddDays(39);
        // Rogation Days: Mon, Tue, Wed before Ascension Thursday
        return date == ascension.AddDays(-3)
            || date == ascension.AddDays(-2)
            || date == ascension.AddDays(-1);
    }

    /// <summary>
    /// Returns the special title for this Sunday, if any. The Last Sunday of Epiphany
    /// is computed directly from Easter (Easter − 49 days, always a Sunday) rather than
    /// from the forward-counted week number, since the number of Epiphany Sundays varies
    /// by year depending on when Ash Wednesday falls.
    /// </summary>
    private static string? GetSundayTitle(DateOnly date, LiturgicalSeason season, int weekNumber, int? properNumber)
    {
        if (date.DayOfWeek != DayOfWeek.Sunday)
        {
            return null;
        }

        if (season == LiturgicalSeason.Epiphany)
        {
            if (weekNumber == 1)
            {
                return "The Baptism of Our Lord";
            }

            var lastSundayOfEpiphany = EasterCalculator.GetEaster(date.Year).AddDays(-49);
            if (date == lastSundayOfEpiphany)
            {
                return "Transfiguration Sunday";
            }
        }

        if (season == LiturgicalSeason.OrdinaryTime && properNumber == 29)
        {
            return "Christ the King";
        }

        return null;
    }

    private static bool IsFastDay(DateOnly date, LiturgicalSeason season)
    {
        // Weekdays of Lent and Holy Week (excluding Sundays)
        if ((season == LiturgicalSeason.Lent || season == LiturgicalSeason.HolyWeek)
            && date.DayOfWeek != DayOfWeek.Sunday)
        {
            return true;
        }

        // Every Friday outside the Twelve Days of Christmas and the Fifty Days of Easter
        if (date.DayOfWeek == DayOfWeek.Friday
            && season != LiturgicalSeason.Christmas
            && season != LiturgicalSeason.Easter
            && season != LiturgicalSeason.Pentecost)
        {
            return true;
        }

        return false;
    }
}
