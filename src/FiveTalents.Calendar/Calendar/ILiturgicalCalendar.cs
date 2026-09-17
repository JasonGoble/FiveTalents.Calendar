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
    /// Returns every rubrically-possible Eucharist occurrence for <paramref name="date"/>,
    /// ranked by <see cref="OccurrenceType"/>, rather than resolving a single answer — see
    /// ADR 0008, ADR 0016. <see cref="LiturgicalDay.Readings"/> is derived from the first
    /// item here (or in <see cref="LiturgicalDay.Occurrences"/> generally) matching, in
    /// order, <see cref="ObservancePrecedence.Prescribed"/>,
    /// <see cref="ObservancePrecedence.CommonPractice"/>, then
    /// <see cref="ObservancePrecedence.Supplementary"/> (see ADR 0015).
    /// </summary>
    public IReadOnlyList<Occurrence> GetPossibleEucharistObservances(DateOnly date);

    /// <summary>Returns the liturgical year that contains <paramref name="date"/>.</summary>
    public int GetLiturgicalYear(DateOnly date);

    /// <summary>Returns the date of Easter Sunday for the given Gregorian year.</summary>
    public DateOnly GetEaster(int year);
}
