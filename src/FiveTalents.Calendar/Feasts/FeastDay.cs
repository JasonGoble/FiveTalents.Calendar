namespace FiveTalents.Calendar.Feasts;

/// <summary>
/// Internal catalog entry for a feast or commemoration. Consumers receive
/// <see cref="Observance"/> instances, which carry resolved readings.
/// </summary>
internal sealed record FeastDay
{
    public required string Name { get; init; }
    public required FeastRank Rank { get; init; }

    /// <summary>
    /// Liturgical color for this observance. Null for commemorations, which
    /// inherit the color of the surrounding season.
    /// </summary>
    public LiturgicalColor? Color { get; init; }

    /// <summary>
    /// True for feasts whose date is computed relative to Easter (Palm Sunday,
    /// Easter Day, Ascension, etc.). Moveable feasts are never subject to the
    /// transfer rule — they define the season rather than interrupting it.
    /// </summary>
    public bool IsMoveable { get; init; }

    /// <summary>
    /// Which Common of Saints applies when this feast has no specific lectionary
    /// readings of its own. Null for Principal Feasts and Major Holy Days that
    /// have their own propers.
    /// </summary>
    public CommemorationCommon? Common { get; init; }
}
