namespace FiveTalents.Calendar.Lectionary;

/// <summary>
/// Resolves a <see cref="LectionaryReading.TranslationCode"/> value to a human-readable
/// Bible edition name.
/// </summary>
public sealed record TranslationInfo
{
    public required string Code { get; init; }

    public required string Name { get; init; }

    /// <summary>
    /// What kind of resource this is — a Bible translation or a Psalter. Determines the
    /// base canon <see cref="AdditionalBooks"/> is relative to.
    /// </summary>
    public required TranslationResourceType ResourceType { get; init; }

    /// <summary>
    /// Books this edition includes beyond the standard canon for its
    /// <see cref="ResourceType"/> (e.g. ESV-A adds the Apocrypha on top of ESV's base
    /// Protestant canon — it is not restricted to only these books). Empty when the
    /// edition doesn't extend beyond the standard canon.
    /// </summary>
    public IReadOnlyList<string> AdditionalBooks { get; init; } = [];
}
