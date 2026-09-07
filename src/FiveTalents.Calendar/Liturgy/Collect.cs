namespace FiveTalents.Calendar.Liturgy;

/// <summary>
/// The proper Collect for a Eucharistic observance. See ADR 0014.
/// </summary>
public sealed record Collect
{
    public required string Text { get; init; }

    /// <summary>
    /// Full alternate Collect texts the BCP offers in place of <see cref="Text"/> (e.g. the
    /// Ember Days' "or this"). Empty when the BCP provides only one. Mirrors
    /// <see cref="Lectionary.LectionaryReading.AlternateCitations"/>'s shape.
    /// </summary>
    public IReadOnlyList<string> AlternateTexts { get; init; } = [];
}
