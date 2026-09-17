using FiveTalents.Calendar.Feasts;
using FiveTalents.Calendar.Lectionary;
using FiveTalents.Calendar.Liturgy;
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

        // Ordered fallback across all three tiers — Prescribed is effectively always present
        // today, so this generalizes the picker defensively rather than changing any observable
        // result. See ADR 0015.
        var resolvedOption = observances.FirstOrDefault(o => o.Precedence == ObservancePrecedence.Prescribed)
            ?? observances.FirstOrDefault(o => o.Precedence == ObservancePrecedence.CommonPractice)
            ?? observances.FirstOrDefault(o => o.Precedence == ObservancePrecedence.Supplementary);

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
            Feast = resolvedOption?.Feast,
            Commemorations = commemorations,
            IsEmberDay = AcnaFeastCatalog.IsEmberDay(date, date.Year),
            IsRogationDay = IsRogationDay(date, date.Year),
            IsFastDay = IsFastDay(date, info.Season),
            ProperNumber = properNumber,
            SundayTitle = GetSundayTitle(date, info.Season, info.WeekNumber, properNumber),
            DailyOffice = AcnaDailyOfficeLectionary.GetReadings(date),
            Readings = resolvedOption?.Services ?? [],
        };
    }

    /// <summary>
    /// Returns every rubrically-possible Eucharist observance for <paramref name="date"/>,
    /// ranked by precedence, instead of resolving a single answer — see ADR 0008.
    /// <see cref="GetDay"/>'s <c>Feast</c>/<c>Readings</c> are derived from the first item
    /// here matching, in order, <see cref="ObservancePrecedence.Prescribed"/>,
    /// <see cref="ObservancePrecedence.CommonPractice"/>, then
    /// <see cref="ObservancePrecedence.Supplementary"/> (see ADR 0015), so this is the single
    /// source of truth for Eucharistic precedence.
    /// </summary>
    public IReadOnlyList<ObservanceOption> GetPossibleEucharistObservances(DateOnly date)
    {
        var info = SeasonResolver.Resolve(date, date.Year);
        int? properNumber = SeasonResolver.GetProperNumber(date, info.Season);

        var holyDays = AcnaFeastCatalog.GetHolyDays(date, date.Year);
        var candidateFeast = holyDays.Count > 0 ? holyDays.MaxBy(f => (int)f.Rank) : null;
        var suppressedHolyDay = AcnaFeastCatalog.GetSuppressedFixedHolyDay(date, date.Year);

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
                Collect = AcnaCollectsAndPrefaces.TryGetCollect(feastKey),
                PrefaceNames = AcnaCollectsAndPrefaces.GetPrefaceNames(feastKey),
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
                string? rubricNote;
                FeastDay? yieldedFeast;
                if (mandatoryYield)
                {
                    rubricNote = $"BCP 2019 p.689: {candidateFeast!.Name} falls today, but Holy Days do not displace the propers of a Sunday in Advent, Lent, or Easter; it may instead be transferred to the nearest following weekday.";
                    yieldedFeast = candidateFeast;
                }
                else if (suppressedHolyDay is not null)
                {
                    rubricNote = $"BCP 2019 p.689: {suppressedHolyDay.Name} falls today, but no holy day or observance can replace the fixed propers of Holy Week or Easter Week.";
                    yieldedFeast = suppressedHolyDay;
                }
                else
                {
                    rubricNote = null;
                    yieldedFeast = null;
                }

                options.Add(new ObservanceOption
                {
                    Feast = attachFeast ? candidateFeast : null,
                    Precedence = ObservancePrecedence.Prescribed,
                    Services = seasonServices,
                    RubricNote = rubricNote,
                    YieldedFeast = yieldedFeast,
                    Collect = AcnaCollectsAndPrefaces.TryGetCollect(seasonKey),
                    PrefaceNames = AcnaCollectsAndPrefaces.GetPrefaceNames(seasonKey),
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
                    Collect = AcnaCollectsAndPrefaces.TryGetCollect(seasonKey),
                    PrefaceNames = AcnaCollectsAndPrefaces.GetPrefaceNames(seasonKey),
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
                    Collect = AcnaCollectsAndPrefaces.TryGetCollect(seasonKey),
                    PrefaceNames = AcnaCollectsAndPrefaces.GetPrefaceNames(seasonKey),
                });
            }
            // A Principal Feast on its own weekday gets no alternative at all — Rule 1
            // (ADR 0006) is absolute, and there's no evidence of real deviation from it.
        }

        // BCP 2019's six National Days are civil observances the rubric never ranks against
        // anything else — always shown, additive, and never participating in candidateFeast's
        // rank comparison above. Jurisdiction is named in the label text itself, not a
        // structured field — ADR 0012/0013 already rejected a jurisdiction concept for this
        // reason. See ADR 0015.
        foreach (var (name, key, resolve) in _nationalDays)
        {
            if (resolve(date.Year) != date)
            {
                continue;
            }

            options.Add(new ObservanceOption
            {
                Feast = new FeastDay { Name = name, Rank = FeastRank.Commemoration },
                Precedence = ObservancePrecedence.Supplementary,
                Services = AcnaSundayLectionary.BuildServicesForKey(key, info.LectionaryYear),
                Collect = AcnaCollectsAndPrefaces.TryGetCollect(key),
                PrefaceNames = AcnaCollectsAndPrefaces.GetPrefaceNames(key),
            });
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

    /// <summary>
    /// BCP 2019's six civil National Days (see ADR 0015). Each entry resolves its own date
    /// independently and shares no rank comparison with <c>candidateFeast</c> above — these
    /// are always additive, never competing. Remembrance Day and Memorial Day are two
    /// distinct entries despite sharing readings/Collect content with no other day; the two
    /// Thanksgivings share one lectionary/Collect key but get distinct labels.
    /// </summary>
    private static readonly (string Name, string Key, Func<int, DateOnly> Resolve)[] _nationalDays =
    [
        ("Thanksgiving Day (Canada)", "NationalDay_ThanksgivingDay", year => NthWeekdayOfMonth(year, 10, DayOfWeek.Monday, 2)),
        ("Thanksgiving Day (United States)", "NationalDay_ThanksgivingDay", year => NthWeekdayOfMonth(year, 11, DayOfWeek.Thursday, 4)),
        ("Canada Day", "NationalDay_CanadaDay", year => new DateOnly(year, 7, 1)),
        ("Independence Day", "NationalDay_IndependenceDay", year => new DateOnly(year, 7, 4)),
        ("Remembrance Day", "NationalDay_RemembranceDay", year => new DateOnly(year, 11, 11)),
        ("Memorial Day", "NationalDay_MemorialDay", year => LastWeekdayOfMonth(year, 5, DayOfWeek.Monday)),
    ];

    /// <summary>Returns the date of the <paramref name="n"/>th <paramref name="dayOfWeek"/> in the given month.</summary>
    private static DateOnly NthWeekdayOfMonth(int year, int month, DayOfWeek dayOfWeek, int n)
    {
        DateOnly first = new DateOnly(year, month, 1);
        int offset = ((int)dayOfWeek - (int)first.DayOfWeek + 7) % 7;
        return first.AddDays(offset + 7 * (n - 1));
    }

    /// <summary>Returns the date of the last <paramref name="dayOfWeek"/> in the given month.</summary>
    private static DateOnly LastWeekdayOfMonth(int year, int month, DayOfWeek dayOfWeek)
    {
        DateOnly lastDay = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        int offset = ((int)lastDay.DayOfWeek - (int)dayOfWeek + 7) % 7;
        return lastDay.AddDays(-offset);
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
