namespace FiveTalents.Calendar.Feasts;

public sealed record FeastDay
{
    public required string Name { get; init; }
    public required FeastRank Rank { get; init; }

    /// <summary>
    /// Liturgical color for this observance. Null for commemorations, which
    /// inherit the color of the surrounding season.
    /// </summary>
    public LiturgicalColor? Color { get; init; }
}
