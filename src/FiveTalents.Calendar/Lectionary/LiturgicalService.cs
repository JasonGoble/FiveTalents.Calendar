namespace FiveTalents.Calendar.Lectionary;

/// <summary>
/// A named set of lectionary readings for a single liturgical service within a day.
/// Most days have one unnamed service; Palm Sunday has two (Liturgy of the Palms and
/// Liturgy of the Word).
/// </summary>
public sealed record LiturgicalService
{
    /// <summary>
    /// Service name, or null when the day has only one service and no distinct name is needed.
    /// </summary>
    public string? Name { get; init; }

    public required IReadOnlyList<LectionaryReading> Readings { get; init; }
}
