using FiveTalents.Calendar.Seasons;

namespace FiveTalents.Calendar.Calendar;

public sealed record LiturgicalWeek
{
    public required LiturgicalSeason Season { get; init; }

    /// <summary>Week number within the season (e.g., 1 = First Sunday of Advent).</summary>
    public required int WeekNumber { get; init; }

    /// <summary>Lectionary year: A, B, or C.</summary>
    public required char LectionaryYear { get; init; }
}
