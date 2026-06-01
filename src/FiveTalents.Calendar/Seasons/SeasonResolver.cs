using FiveTalents.Calendar.Calendar;

namespace FiveTalents.Calendar.Seasons;

internal static class SeasonResolver
{
    /// <summary>
    /// Returns the First Sunday of Advent for the liturgical year that begins in
    /// the given Gregorian year. Advent starts on the Sunday nearest to Nov 30,
    /// which is always the 4th Sunday before Christmas.
    /// </summary>
    public static DateOnly GetAdventSunday(int year)
    {
        // Walk back from Christmas to the Sunday that starts the Christmas week,
        // then back 3 more weeks to reach the First Sunday of Advent.
        DateOnly christmas = new DateOnly(year, 12, 25);
        int daysFromSunday = (int)christmas.DayOfWeek; // Sunday = 0
        var sundayBeforeChristmas = christmas.AddDays(-daysFromSunday);
        if (daysFromSunday == 0)
        {
            sundayBeforeChristmas = christmas.AddDays(-7);
        }

        return sundayBeforeChristmas.AddDays(-21);
    }

    /// <summary>
    /// Returns the lectionary year (A, B, or C) for the liturgical year that
    /// begins on the given Advent Sunday.
    /// </summary>
    public static char GetLectionaryYear(DateOnly adventSunday)
    {
        // Year A: Gregorian year of Advent Sunday ≡ 0 (mod 3)  → 2025, 2028, …
        // Year B: ≡ 1 (mod 3)  → 2023, 2026, …
        // Year C: ≡ 2 (mod 3)  → 2024, 2027, …
        return (adventSunday.Year % 3) switch
        {
            0 => 'A',
            1 => 'B',
            _ => 'C',
        };
    }

    public sealed record SeasonInfo(
        LiturgicalSeason Season,
        int WeekNumber,
        char LectionaryYear);

    public static SeasonInfo Resolve(DateOnly date, int gregorianYear)
    {
        var easter = EasterCalculator.GetEaster(gregorianYear);

        // ── Key moveable dates ────────────────────────────────────────────────
        var ashWednesday = easter.AddDays(-46);
        var palmSunday = easter.AddDays(-7);
        var pentecost = easter.AddDays(49);

        var adventThisYear = GetAdventSunday(gregorianYear);
        var adventPrevYear = GetAdventSunday(gregorianYear - 1);

        // ── Governing liturgical year → lectionary year ──────────────────────
        var governingAdvent = date >= adventThisYear ? adventThisYear : adventPrevYear;
        char lectionaryYear = GetLectionaryYear(governingAdvent);

        // ── Season and week ──────────────────────────────────────────────────

        // Advent: First Sunday of Advent through Christmas Eve
        DateOnly christmasEve = new DateOnly(gregorianYear, 12, 24);
        if (date >= adventThisYear && date <= christmasEve)
        {
            return new SeasonInfo(LiturgicalSeason.Advent, WeekOfSeason(date, adventThisYear), lectionaryYear);
        }

        // Christmas: Dec 25 – Jan 5 (spanning the year boundary)
        // Week numbering: week 1 = Dec 25–31, week 2 = Jan 1–5
        DateOnly christmasStart = new DateOnly(gregorianYear, 12, 25);
        if (date >= christmasStart)
        {
            int christmasDay = date.Day - 24; // Dec 25 = day 1, Dec 31 = day 7
            return new SeasonInfo(LiturgicalSeason.Christmas, (christmasDay - 1) / 7 + 1, lectionaryYear);
        }

        if (date.Month == 1 && date.Day <= 5)
        {
            int christmasDay = date.Day + 7; // Jan 1 = day 8, Jan 5 = day 12
            return new SeasonInfo(LiturgicalSeason.Christmas, (christmasDay - 1) / 7 + 1, lectionaryYear);
        }

        // Epiphany: Jan 6 – day before Ash Wednesday
        // Week 0 = Jan 6 through the day before the First Sunday of Epiphany ("Days After Epiphany")
        // Week 1 = First Sunday of Epiphany onward
        DateOnly epiphany = new DateOnly(gregorianYear, 1, 6);
        if (date >= epiphany && date < ashWednesday)
        {
            return new SeasonInfo(LiturgicalSeason.Epiphany, WeekOfSeason(date, epiphany), lectionaryYear);
        }

        // Lent: Ash Wednesday – Saturday before Palm Sunday
        // Week 0 = Ash Wednesday through Saturday before First Sunday of Lent
        // Week 1 = First Sunday of Lent onward
        if (date >= ashWednesday && date < palmSunday)
        {
            return new SeasonInfo(LiturgicalSeason.Lent, WeekOfSeason(date, ashWednesday), lectionaryYear);
        }

        // Holy Week: Palm Sunday – Holy Saturday (always week 1)
        var holySaturday = easter.AddDays(-1);
        if (date >= palmSunday && date <= holySaturday)
        {
            return new SeasonInfo(LiturgicalSeason.HolyWeek, 1, lectionaryYear);
        }

        // Eastertide: Easter Sunday – Saturday before Pentecost
        var pentecostEve = pentecost.AddDays(-1);
        if (date >= easter && date <= pentecostEve)
        {
            return new SeasonInfo(LiturgicalSeason.Easter, WeekOfSeason(date, easter), lectionaryYear);
        }

        // Pentecost Sunday (always week 1)
        if (date == pentecost)
        {
            return new SeasonInfo(LiturgicalSeason.Pentecost, 1, lectionaryYear);
        }

        // Season after Pentecost: day after Pentecost – Advent Eve
        // Week 0 = Mon–Sat between Pentecost and Trinity Sunday ("After Pentecost" days)
        // Week 1 = Trinity Sunday onward
        var adventEve = adventThisYear.AddDays(-1);
        if (date > pentecost && date <= adventEve)
        {
            var trinitySunday = pentecost.AddDays(7);
            return new SeasonInfo(LiturgicalSeason.OrdinaryTime, WeekOfSeason(date, trinitySunday), lectionaryYear);
        }

        // Fallback — should not be reached for valid dates
        return new SeasonInfo(LiturgicalSeason.OrdinaryTime, 0, lectionaryYear);
    }

    /// <summary>
    /// Returns the BCP Proper number (1–29) for any day in the Season after Pentecost,
    /// or null if the date is not in OrdinaryTime. Weekdays use the Proper of their
    /// preceding Sunday. Propers 1–2 are weekday-only (no Sunday falls in those ranges);
    /// Proper 3 onwards include a Sunday.
    /// </summary>
    public static int? GetProperNumber(DateOnly date, LiturgicalSeason season)
    {
        if (season != LiturgicalSeason.OrdinaryTime)
        {
            return null;
        }

        // Find the governing Sunday (most recent Sunday on or before this date)
        int daysSinceSunday = (int)date.DayOfWeek;
        DateOnly governingSunday = date.AddDays(-daysSinceSunday);

        DateOnly may8 = new DateOnly(governingSunday.Year, 5, 8);
        int daysSinceMay8 = governingSunday.DayNumber - may8.DayNumber;
        if (daysSinceMay8 < 0)
        {
            return null;
        }

        int proper = (daysSinceMay8 / 7) + 1;
        return proper is >= 1 and <= 29 ? proper : null;
    }

    /// <summary>
    /// Returns the 1-based week number for <paramref name="date"/> within a season,
    /// where the first Sunday on or after <paramref name="seasonStart"/> is week 1.
    /// Days before that first Sunday return 0 (e.g. "Days After Epiphany",
    /// "After Ash Wednesday", "After Pentecost").
    /// </summary>
    public static int WeekOfSeason(DateOnly date, DateOnly seasonStart)
    {
        // Forward-normalise to the first Sunday on or after seasonStart
        int daysToFirstSunday = (7 - (int)seasonStart.DayOfWeek) % 7;
        var firstSunday = seasonStart.AddDays(daysToFirstSunday);

        if (date < firstSunday)
        {
            return 0;
        }

        return (date.DayNumber - firstSunday.DayNumber) / 7 + 1;
    }
}
