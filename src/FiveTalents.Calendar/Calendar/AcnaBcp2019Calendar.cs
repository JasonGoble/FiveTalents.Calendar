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

        var primaryFeast = holyDays.Count > 0
            ? holyDays.MaxBy(f => (int)f.Rank)
            : null;

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

    private static bool IsRogationDay(DateOnly date, int year)
    {
        var ascension = EasterCalculator.GetEaster(year).AddDays(39);
        // Rogation Days: Mon, Tue, Wed before Ascension Thursday
        return date == ascension.AddDays(-3)
            || date == ascension.AddDays(-2)
            || date == ascension.AddDays(-1);
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
