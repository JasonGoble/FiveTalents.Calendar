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

        int? properNumber = SeasonResolver.GetProperNumber(date, info.Season);

        // Build the shell day first — needed to resolve season-proper readings.
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
            IsEmberDay = AcnaFeastCatalog.IsEmberDay(date, date.Year),
            IsRogationDay = IsRogationDay(date, date.Year),
            IsFastDay = IsFastDay(date, info.Season),
            ProperNumber = properNumber,
        };

        return day with { Observances = BuildObservances(day, holyDays, commemorations, info) };
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

    // ── Observance building ───────────────────────────────────────────────────

    private static IReadOnlyList<Observance> BuildObservances(
        LiturgicalDay day,
        IReadOnlyList<FeastDay> holyDays,
        IReadOnlyList<FeastDay> commemorations,
        SeasonResolver.SeasonInfo info)
    {
        bool isSunday = day.Date.DayOfWeek == DayOfWeek.Sunday;

        // A Holy Day is transferred when it falls on a Sunday in Advent, Lent, or Easter.
        bool transferSeason =
            isSunday &&
            day.Season is LiturgicalSeason.Advent or LiturgicalSeason.Lent or LiturgicalSeason.Easter;

        List<Observance> observances = new List<Observance>();

        // 1. Holy Days (Major / Principal) — all of them, not just the highest-ranked winner.
        foreach (var feast in holyDays.OrderByDescending(f => (int)f.Rank))
        {
            // Moveable feasts define the season itself and are never transferred.
            bool transferred = transferSeason && !feast.IsMoveable;
            var readings = transferred
                ? []
                : AcnaSundayLectionary.GetReadingsForFeast(feast, info.LectionaryYear);

            observances.Add(new Observance
            {
                Name = feast.Name,
                Rank = feast.Rank,
                Color = feast.Color,
                IsTransferred = transferred,
                ReadingOptions = readings,
            });
        }

        // 2. Season Sunday proper — only on Sundays that have a named proper.
        if (isSunday)
        {
            string? properName = AcnaSundayLectionary.GetSeasonProperName(day);
            if (properName is not null)
            {
                // Determine the season color for the proper observance.
                LiturgicalColor seasonColor = SeasonColor(day.Season);

                // Use the Holy Day readings for the proper when feasts are transferred,
                // otherwise use the season readings directly.
                var properReadings = transferSeason && holyDays.Count > 0
                    ? AcnaSundayLectionary.GetSeasonProperReadings(day)
                    : AcnaSundayLectionary.GetSeasonProperReadings(day);

                // Rank: season proper sits below Principal/Major feasts; use Minor as placeholder.
                observances.Add(new Observance
                {
                    Name = properName,
                    Rank = FeastRank.Minor,
                    Color = seasonColor,
                    IsTransferred = false,
                    ReadingOptions = properReadings,
                });
            }
        }

        // 3. Commemorations — each gets readings from its Common of Saints.
        foreach (var comm in commemorations)
        {
            var readings = AcnaSundayLectionary.GetReadingsForFeast(comm, info.LectionaryYear);
            observances.Add(new Observance
            {
                Name = comm.Name,
                Rank = comm.Rank,
                Color = comm.Color,
                IsTransferred = false,
                ReadingOptions = readings,
            });
        }

        // Sort: non-transferred observances by rank descending, transferred last.
        return observances
            .OrderBy(o => o.IsTransferred ? 1 : 0)
            .ThenByDescending(o => (int)o.Rank)
            .ToList();
    }

    private static LiturgicalColor SeasonColor(LiturgicalSeason season) => season switch
    {
        LiturgicalSeason.Advent => LiturgicalColor.Purple,
        LiturgicalSeason.Christmas => LiturgicalColor.White,
        LiturgicalSeason.Epiphany => LiturgicalColor.White,
        LiturgicalSeason.Lent => LiturgicalColor.Purple,
        LiturgicalSeason.HolyWeek => LiturgicalColor.Purple,
        LiturgicalSeason.Easter => LiturgicalColor.White,
        LiturgicalSeason.Pentecost => LiturgicalColor.Red,
        LiturgicalSeason.OrdinaryTime => LiturgicalColor.Green,
        _ => LiturgicalColor.White,
    };

    // ── Private helpers ───────────────────────────────────────────────────────

    private static bool IsRogationDay(DateOnly date, int year)
    {
        var ascension = EasterCalculator.GetEaster(year).AddDays(39);
        return date == ascension.AddDays(-3)
            || date == ascension.AddDays(-2)
            || date == ascension.AddDays(-1);
    }

    private static bool IsFastDay(DateOnly date, LiturgicalSeason season)
    {
        if ((season == LiturgicalSeason.Lent || season == LiturgicalSeason.HolyWeek)
            && date.DayOfWeek != DayOfWeek.Sunday)
        {
            return true;
        }

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
