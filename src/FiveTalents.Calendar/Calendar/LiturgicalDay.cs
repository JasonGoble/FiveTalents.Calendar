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
    /// Every occurrence this day carries — Feasts, the Sunday's own slot, commemorations,
    /// Ember/Rogation Days, and season-day naming — ordered by <see cref="OccurrenceType"/>.
    /// Index 0 is always the highest-precedence occurrence when more than one is Prescribed
    /// or CommonPractice. Supersedes the old separate <c>Feast</c>/<c>Commemorations</c>/
    /// <c>SundayTitle</c> fields, which could only ever surface one winning answer even on
    /// dates where several occurrences genuinely coexist (e.g. a Major Feast landing on a
    /// Sunday that is also a named day of Christmastide). See ADR 0008, ADR 0016.
    /// </summary>
    public IReadOnlyList<Occurrence> Occurrences { get; init; } = [];

    /// <summary>
    /// Liturgical services for this day, each with their own set of readings. Derived from
    /// the first item of <see cref="Occurrences"/> matching, in order,
    /// <see cref="ObservancePrecedence.Prescribed"/>, <see cref="ObservancePrecedence.CommonPractice"/>,
    /// then <see cref="ObservancePrecedence.Supplementary"/> — see ADR 0008/0015/0016. Most
    /// days have one unnamed service. Palm Sunday has two named services: "Liturgy of the
    /// Palms" and "Liturgy of the Word".
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
