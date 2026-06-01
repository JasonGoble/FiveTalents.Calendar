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

    /// <summary>
    /// The Principal Feast or Holy Day (rank Major or above) for this day, if any.
    /// When a fixed feast conflicts with a Sunday, the higher-ranked observance is returned.
    /// </summary>
    public FeastDay? Feast { get; init; }

    /// <summary>
    /// Optional commemorations (Anglican and Ecumenical) observed on this day.
    /// These do not displace the season or a higher-ranked feast.
    /// </summary>
    public IReadOnlyList<FeastDay> Commemorations { get; init; } = [];

    /// <summary>Lectionary readings assigned to this day.</summary>
    public IReadOnlyList<LectionaryReading> Readings { get; init; } = [];

    /// <summary>
    /// True on the twelve Ember Days per year (Wed, Fri, Sat after the First Sunday
    /// of Lent, after Pentecost, after Holy Cross Day, and after St. Lucy's Day).
    /// </summary>
    public bool IsEmberDay { get; init; }

    /// <summary>
    /// True on the three Rogation Days (Mon, Tue, Wed before Ascension Day).
    /// </summary>
    public bool IsRogationDay { get; init; }

    /// <summary>
    /// True on days encouraged as fasts: weekdays of Lent and Holy Week, and every
    /// Friday outside the Twelve Days of Christmas and the Fifty Days of Easter.
    /// </summary>
    public bool IsFastDay { get; init; }
}
