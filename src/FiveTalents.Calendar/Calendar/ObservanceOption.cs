using FiveTalents.Calendar.Feasts;
using FiveTalents.Calendar.Lectionary;

namespace FiveTalents.Calendar.Calendar;

/// <summary>
/// One rubrically-possible observance for a given date and service, with its own
/// readings. <see cref="ILiturgicalCalendar"/> returns these as a ranked list rather than
/// resolving one answer, so a consuming application can make its own pastoral choice
/// among what the BCP actually permits. See ADR 0008.
/// </summary>
public sealed record ObservanceOption
{
    /// <summary>The named Feast this option observes, or null for the season's own propers.</summary>
    public FeastDay? Feast { get; init; }

    public required ObservancePrecedence Precedence { get; init; }

    public required IReadOnlyList<LiturgicalService> Services { get; init; }

    /// <summary>
    /// Explains why an option is absent or constrained, when that's not otherwise obvious
    /// from the option list alone (e.g. a Holy Day that yielded to a governing Sunday).
    /// Null when there's nothing that needs explaining.
    /// </summary>
    public string? RubricNote { get; init; }
}
