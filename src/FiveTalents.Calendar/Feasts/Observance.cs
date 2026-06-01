using FiveTalents.Calendar.Lectionary;

namespace FiveTalents.Calendar.Feasts;

/// <summary>
/// A single liturgical observance on a given day, with its resolved readings.
/// A day may have multiple observances (e.g. a Principal Feast and a Holy Day
/// that fall on the same date, or a Holy Day alongside the season Sunday proper).
/// Observances are ordered by effective priority, descending.
/// </summary>
public sealed record Observance
{
    public required string Name { get; init; }
    public required FeastRank Rank { get; init; }

    /// <summary>
    /// Liturgical color for this observance. Null for commemorations, which
    /// inherit the color of the surrounding season.
    /// </summary>
    public LiturgicalColor? Color { get; init; }

    /// <summary>
    /// True when a Holy Day has been transferred off a Sunday in Advent, Lent,
    /// or Easter. The observance is still listed so the priest knows what to
    /// celebrate on the transferred weekday, but it sorts below the season proper.
    /// </summary>
    public bool IsTransferred { get; init; }

    /// <summary>
    /// The available sets of readings for this observance. Most have one set;
    /// occasions where the BCP provides alternatives (e.g. Christmas Day I/II/III)
    /// have multiple, each identified by <see cref="ReadingSet.Label"/>.
    /// </summary>
    public IReadOnlyList<ReadingSet> ReadingOptions { get; init; } = [];
}
