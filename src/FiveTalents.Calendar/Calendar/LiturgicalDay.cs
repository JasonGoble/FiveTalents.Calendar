using FiveTalents.Calendar.Feasts;
using FiveTalents.Calendar.Lectionary;
using FiveTalents.Calendar.Seasons;

namespace FiveTalents.Calendar.Calendar;

/// <summary>
/// Represents a single day on the liturgical calendar, resolved against a specific tradition.
/// </summary>
public sealed record LiturgicalDay
{
    public required DateOnly Date { get; init; }
    public required LiturgicalSeason Season { get; init; }
    public required LiturgicalWeek Week { get; init; }

    /// <summary>The named feast or observance for this day, if any.</summary>
    public FeastDay? Feast { get; init; }

    /// <summary>Lectionary readings assigned to this day.</summary>
    public IReadOnlyList<LectionaryReading> Readings { get; init; } = [];
}
