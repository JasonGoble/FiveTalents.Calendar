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
    /// Shorter or alternate citations provided by the BCP (e.g. a truncated passage, or an
    /// "or" alternative). Empty when none exist. Most readings have zero or one entry;
    /// a few (e.g. the Palm Sunday Psalm) have more.
    /// </summary>
    public IReadOnlyList<string> AlternateCitations { get; init; } = [];

    /// <summary>
    /// Optional translation identifier when the citation follows a non-default versification
    /// (e.g. "LXX" for Septuagint Psalm numbering). Null for the default Hebrew/Masoretic text.
    /// </summary>
    public string? TranslationCode { get; init; }
}
