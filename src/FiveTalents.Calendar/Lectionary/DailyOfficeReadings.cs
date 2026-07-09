namespace FiveTalents.Calendar.Lectionary;

/// <summary>
/// Morning and Evening Prayer readings for a single calendar date. Present for every
/// day of the year, independent of <see cref="LiturgicalDay.Readings"/> (the Sunday/Holy
/// Day Eucharist lectionary), which is often empty on ordinary weekdays.
/// </summary>
public sealed record DailyOfficeReadings
{
    public required LiturgicalService MorningPrayer { get; init; }

    public required LiturgicalService EveningPrayer { get; init; }
}
