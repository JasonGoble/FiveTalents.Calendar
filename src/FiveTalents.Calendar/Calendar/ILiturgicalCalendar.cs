namespace FiveTalents.Calendar.Calendar;

/// <summary>
/// Core abstraction for resolving dates against a liturgical tradition.
/// </summary>
public interface ILiturgicalCalendar
{
    public LiturgicalTradition Tradition { get; }

    public LiturgicalDay GetDay(DateOnly date);
    public IReadOnlyList<LiturgicalDay> GetRange(DateOnly from, DateOnly to);

    /// <summary>Returns the liturgical year that contains <paramref name="date"/>.</summary>
    public int GetLiturgicalYear(DateOnly date);

    /// <summary>Returns the date of Easter Sunday for the given Gregorian year.</summary>
    public DateOnly GetEaster(int year);
}
