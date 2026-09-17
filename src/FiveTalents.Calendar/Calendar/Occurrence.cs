using FiveTalents.Calendar.Feasts;
using FiveTalents.Calendar.Lectionary;
using FiveTalents.Calendar.Liturgy;

namespace FiveTalents.Calendar.Calendar;

/// <summary>
/// One rubrically-possible occurrence for a given date, with its own readings.
/// <see cref="ILiturgicalCalendar"/> returns these as an ordered stack rather than
/// resolving one answer, so a consuming application can render everything the BCP
/// actually has to say about a day. Supersedes ADR 0008's <c>ObservanceOption</c>. See
/// ADR 0016.
/// </summary>
public sealed record Occurrence
{
    /// <summary>Where this occurrence sits in the ordered stack. See <see cref="OccurrenceType"/>.</summary>
    public required OccurrenceType Type { get; init; }

    /// <summary>
    /// Display name — a Feast's name, a Sunday's title, "The Third Day of Christmas". Null
    /// when nothing nameable exists yet (the ordinary-weekday CommonPractice alternative;
    /// today's Ember Day/Antiphon placeholders).
    /// </summary>
    public string? Name { get; init; }

    /// <summary>The named Feast this occurrence observes, or null when it isn't Feast-shaped.</summary>
    public FeastDay? Feast { get; init; }

    public required ObservancePrecedence Precedence { get; init; }

    public IReadOnlyList<LiturgicalService> Services { get; init; } = [];

    /// <summary>
    /// Explains why an occurrence is absent, constrained, or additionally offered, when
    /// that's not otherwise obvious from the list alone (e.g. a Holy Day that yielded to a
    /// governing Sunday, or All Saints' Day additionally observable on the Sunday following
    /// Nov 1). Null when there's nothing that needs explaining.
    /// </summary>
    public string? RubricNote { get; init; }

    /// <summary>
    /// The fixed Holy Day that yielded to this occurrence's propers per BCP 2019 p.689 —
    /// either a non-Principal Holy Day colliding with a Sunday of Advent, Lent, or Easter
    /// (which never displaces it), or a fixed Holy Day suppressed outright by the Holy
    /// Week/Easter Week rule. Distinct from <see cref="Feast"/>, which names the Feast an
    /// occurrence is *for* — this names one that was excluded. Null in every other case,
    /// including the ordinary-Sunday collision (both Feast and Sunday become their own
    /// Prescribed occurrences there, so nothing is "yielded"). See ADR 0010/0011 and
    /// issues #30/#43/#47.
    /// </summary>
    public FeastDay? YieldedFeast { get; init; }

    /// <summary>The proper Collect for this occurrence, or null when unsourced. See ADR 0014.</summary>
    public Collect? Collect { get; init; }

    /// <summary>Preface catalog names for this occurrence, or empty when unsourced. See ADR 0014.</summary>
    public IReadOnlyList<string> PrefaceNames { get; init; } = [];
}
