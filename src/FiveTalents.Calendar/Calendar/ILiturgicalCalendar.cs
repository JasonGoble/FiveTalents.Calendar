namespace FiveTalents.Calendar.Calendar;

/// <summary>
/// Core abstraction for resolving dates against a liturgical tradition.
/// </summary>
public interface ILiturgicalCalendar
{
    public LiturgicalTradition Tradition { get; }

    public LiturgicalDay GetDay(DateOnly date);
    public IReadOnlyList<LiturgicalDay> GetRange(DateOnly from, DateOnly to);

    /// <summary>
    /// Returns every rubrically-possible Eucharist observance for <paramref name="date"/>,
    /// ranked by precedence, rather than resolving a single answer — see ADR 0008.
    /// <see cref="LiturgicalDay.Feast"/>/<see cref="LiturgicalDay.Readings"/> are the first
    /// <see cref="ObservancePrecedence.Prescribed"/> item of this list.
    /// </summary>
    public IReadOnlyList<ObservanceOption> GetPossibleEucharistObservances(DateOnly date);

    /// <summary>Returns the liturgical year that contains <paramref name="date"/>.</summary>
    public int GetLiturgicalYear(DateOnly date);

    /// <summary>Returns the date of Easter Sunday for the given Gregorian year.</summary>
    public DateOnly GetEaster(int year);
}
