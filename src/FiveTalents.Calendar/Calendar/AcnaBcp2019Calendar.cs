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
        var commemorations = AcnaFeastCatalog.GetCommemorations(date, date.Year);
        int? properNumber = SeasonResolver.GetProperNumber(date, info.Season);

        var observances = GetPossibleEucharistObservances(date);
        var prescribed = observances.FirstOrDefault(o => o.Precedence == ObservancePrecedence.Prescribed);

        return new LiturgicalDay
        {
            Date = date,
            Season = info.Season,
            Week = new LiturgicalWeek
            {
                Season = info.Season,
                WeekNumber = info.WeekNumber,
                LectionaryYear = info.LectionaryYear,
            },
            Feast = prescribed?.Feast,
            Commemorations = commemorations,
            IsEmberDay = AcnaFeastCatalog.IsEmberDay(date, date.Year),
            IsRogationDay = IsRogationDay(date, date.Year),
            IsFastDay = IsFastDay(date, info.Season),
            ProperNumber = properNumber,
            SundayTitle = GetSundayTitle(date, info.Season, info.WeekNumber, properNumber),
            DailyOffice = AcnaDailyOfficeLectionary.GetReadings(date),
            Readings = prescribed?.Services ?? [],
        };
    }

    /// <summary>
    /// Returns every rubrically-possible Eucharist observance for <paramref name="date"/>,
    /// ranked by precedence, instead of resolving a single answer — see ADR 0008.
    /// <see cref="GetDay"/>'s <c>Feast</c>/<c>Readings</c> are derived from the first
    /// <see cref="ObservancePrecedence.Prescribed"/> item here, so this is the single
    /// source of truth for Eucharistic precedence.
    /// </summary>
    public IReadOnlyList<ObservanceOption> GetPossibleEucharistObservances(DateOnly date)
    {
        var info = SeasonResolver.Resolve(date, date.Year);
        int? properNumber = SeasonResolver.GetProperNumber(date, info.Season);

        var holyDays = AcnaFeastCatalog.GetHolyDays(date, date.Year);
        var candidateFeast = holyDays.Count > 0 ? holyDays.MaxBy(f => (int)f.Rank) : null;

        string? seasonKey = AcnaSundayLectionary.GetSeasonKey(date, info.Season, info.WeekNumber, properNumber);
        string? feastKey = candidateFeast is not null ? AcnaSundayLectionary.TryGetFeastKey(candidateFeast) : null;

        // BCP 2019 p.689: a non-Principal Holy Day falling on a Sunday of Advent, Lent, or
        // Easter yields to that Sunday's own propers entirely — the rubric grants no choice
        // here, unlike an ordinary Sunday collision (see below).
        bool mandatoryYield = date.DayOfWeek == DayOfWeek.Sunday
            && info.Season is LiturgicalSeason.Advent or LiturgicalSeason.Lent or LiturgicalSeason.Easter
            && candidateFeast is not null
            && candidateFeast.Rank != FeastRank.Principal;

        // The Feast is its own distinct option only when it has propers of its own that
        // differ from the season's (a Principal Feast that owns its Sunday outright, e.g.
        // Trinity Sunday, resolves to the same key both ways — see ADR 0008).
        bool feastIsDistinctOption = candidateFeast is not null && feastKey is not null && feastKey != seasonKey && !mandatoryYield;

        List<ObservanceOption> options = new List<ObservanceOption>();

        if (feastIsDistinctOption)
        {
            var feastServices = AcnaSundayLectionary.BuildServicesForKey(feastKey, info.LectionaryYear);
            options.Add(new ObservanceOption
            {
                Feast = candidateFeast,
                Precedence = ObservancePrecedence.Prescribed,
                Services = feastServices,
            });
        }

        var seasonServices = seasonKey is not null
            ? AcnaSundayLectionary.BuildServicesForKey(seasonKey, info.LectionaryYear)
            : [];

        if (!feastIsDistinctOption)
        {
            // No competing Feast, the Feast's own key already covers the season's slot
            // (e.g. Trinity Sunday/Easter Day), or the Feast simply has no lectionary
            // entry of its own (e.g. the Epiphany on a weekday) — either way there is
            // exactly one thing to say about this date, whether or not lectionary data
            // happens to resolve for it (a Feast is still "the answer" even on a date
            // with no JSON readings behind it, matching pre-ADR-0008 behavior).
            bool attachFeast = candidateFeast is not null && !mandatoryYield;
            if (attachFeast || seasonServices.Count > 0)
            {
                options.Add(new ObservanceOption
                {
                    Feast = attachFeast ? candidateFeast : null,
                    Precedence = ObservancePrecedence.Prescribed,
                    Services = seasonServices,
                    RubricNote = mandatoryYield
                        ? $"BCP 2019 p.689: {candidateFeast!.Name} falls today, but Holy Days do not displace the propers of a Sunday in Advent, Lent, or Easter."
                        : null,
                });
            }
        }
        else if (seasonServices.Count > 0)
        {
            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                // p.689: a Holy Day colliding with an ordinary Sunday may be observed that
                // Sunday or transferred — the rubric grants an explicit, equal choice.
                options.Add(new ObservanceOption
                {
                    Precedence = ObservancePrecedence.Prescribed,
                    Services = seasonServices,
                });
            }
            else if (candidateFeast!.Rank != FeastRank.Principal)
            {
                // A Red-Letter Day on its own weekday is BCP-directed (Prescribed);
                // skipping it for the ordinary reading isn't rubric-sanctioned, but is
                // real, practiced deviation — surfaced, not hidden.
                options.Add(new ObservanceOption
                {
                    Precedence = ObservancePrecedence.CommonPractice,
                    Services = seasonServices,
                });
            }
            // A Principal Feast on its own weekday gets no alternative at all — Rule 1
            // (ADR 0006) is absolute, and there's no evidence of real deviation from it.
        }

        return options;
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
