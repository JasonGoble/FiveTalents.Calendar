namespace FiveTalents.Calendar.Feasts;

public sealed record FeastDay
{
    public required string Name { get; init; }
    public required FeastRank Rank { get; init; }

    /// <summary>Colour of the liturgical vestments/hangings for this observance.</summary>
    public required LiturgicalColour Colour { get; init; }
}
