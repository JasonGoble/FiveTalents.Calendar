namespace FiveTalents.Calendar.Calendar;

/// <summary>
/// Core abstraction for resolving dates against a liturgical tradition.
/// </summary>
public interface ILiturgicalCalendar
{
    LiturgicalTradition Tradition { get; }

    LiturgicalDay GetDay(DateOnly date);
    IReadOnlyList<LiturgicalDay> GetRange(DateOnly from, DateOnly to);

    /// <summary>Returns the liturgical year that contains <paramref name="date"/>.</summary>
    int GetLiturgicalYear(DateOnly date);

    /// <summary>Returns the date of Easter Sunday for the given Gregorian year.</summary>
    DateOnly GetEaster(int year);
}
