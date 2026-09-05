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
    /// Explains why an option is absent, constrained, or additionally offered, when that's
    /// not otherwise obvious from the option list alone (e.g. a Holy Day that yielded to a
    /// governing Sunday, or All Saints' Day additionally observable on the Sunday following
    /// Nov 1). Null when there's nothing that needs explaining.
    /// </summary>
    public string? RubricNote { get; init; }

    /// <summary>
    /// The fixed Holy Day that yielded to this option's propers per BCP 2019 p.689 — either
    /// a non-Principal Holy Day colliding with a Sunday of Advent, Lent, or Easter (which
    /// never displaces it), or a fixed Holy Day suppressed outright by the Holy Week/Easter
    /// Week rule. Distinct from <see cref="Feast"/>, which names the Feast an option is
    /// *for* — this names one that was excluded. Null in every other case, including the
    /// ordinary-Sunday collision (both Feast and Sunday become their own Prescribed options
    /// there, so nothing is "yielded"). See ADR 0010/0011 and issues #30/#43/#47.
    /// </summary>
    public FeastDay? YieldedFeast { get; init; }
}
