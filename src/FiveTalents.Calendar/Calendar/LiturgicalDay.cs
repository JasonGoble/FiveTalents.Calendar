using FiveTalents.Calendar.Feasts;
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
    /// All liturgical observances for this day, ordered by effective priority descending.
    /// Includes Principal Feasts, Major Holy Days, the season Sunday proper (on Sundays),
    /// and optional commemorations. Transferred feasts appear last.
    /// </summary>
    public IReadOnlyList<Observance> Observances { get; init; } = [];

    /// <summary>
    /// The BCP Proper number (1-29) governing this day's readings in the Season after
    /// Pentecost. Null outside OrdinaryTime. Weekdays share the Proper of their
    /// preceding Sunday.
    /// </summary>
    public int? ProperNumber { get; init; }

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
