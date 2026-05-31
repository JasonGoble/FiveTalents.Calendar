namespace FiveTalents.Calendar.Lectionary;

public sealed record LectionaryReading
{
    public required ReadingType Type { get; init; }

    /// <summary>e.g. "Isaiah 40:1-11"</summary>
    public required string Citation { get; init; }
}
