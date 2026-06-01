namespace FiveTalents.Calendar.Lectionary;

public sealed record LectionaryReading
{
    public required ReadingType Type { get; init; }

    /// <summary>
    /// Primary (full) citation in standard Bible reference format, e.g. "Isa 40:1-11".
    /// Parenthetical notation is expanded: "Luke 2:1-14(15-20)" becomes "Luke 2:1-20".
    /// </summary>
    public required string Citation { get; init; }

    /// <summary>
    /// Shorter or alternate citation, if the BCP provides one.
    /// For parenthetical readings, this is the reading without the optional portion.
    /// For "or" alternatives, this is the second passage. Null when no alternate exists.
    /// </summary>
    public string? AlternateCitation { get; init; }
}
