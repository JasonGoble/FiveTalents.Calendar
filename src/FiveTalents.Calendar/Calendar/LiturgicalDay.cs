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

    /// <summary>
    /// The special title for this Sunday, if any, distinct from its ordinal week
    /// designation — e.g. "The Baptism of Our Lord" for the First Sunday of Epiphany,
    /// "Transfiguration Sunday" for the Last Sunday of Epiphany, "Christ the King" for
    /// the Last Sunday After Pentecost. Null for Sundays without a special title and
    /// for non-Sunday days. Feasts that already carry their own name (e.g. Trinity
    /// Sunday) are exposed via <see cref="Feast"/> instead and do not set this property.
    /// </summary>
    public string? SundayTitle { get; init; }

    /// <summary>
    /// Liturgical services for this day, each with their own set of readings.
    /// Most days have one unnamed service. Palm Sunday has two named services:
    /// "Liturgy of the Palms" and "Liturgy of the Word".
    /// </summary>
    public IReadOnlyList<LiturgicalService> Readings { get; init; } = [];

    /// <summary>
    /// Morning and Evening Prayer readings for this date. Unlike <see cref="Readings"/>
    /// (the Sunday/Holy Day Eucharist lectionary, often empty on ordinary weekdays), this
    /// is always populated — the Daily Office is prayed every day of the year.
    /// </summary>
    public required DailyOfficeReadings DailyOffice { get; init; }

    /// <summary>
    /// The BCP Proper number (1–29) governing this day's readings in the Season after
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
