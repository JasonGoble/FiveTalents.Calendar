namespace FiveTalents.Calendar.Lectionary;

/// <summary>
/// A single set of lectionary readings for a liturgical occasion, optionally labeled
/// when the BCP provides multiple alternative sets (e.g. Christmas Day I, II, III).
/// When only one set is available, <see cref="Label"/> is null.
/// </summary>
public sealed record ReadingSet
{
    /// <summary>
    /// Identifies this set when the BCP provides multiple alternatives (e.g. "I", "II", "III").
    /// Null when the occasion has only one set of readings.
    /// </summary>
    public string? Label { get; init; }

    public IReadOnlyList<LectionaryReading> Readings { get; init; } = [];
}
