namespace FiveTalents.Calendar.Calendar;

/// <summary>
/// Whether an <see cref="ObservanceOption"/> is sanctioned by the BCP rubrics, or merely
/// practiced despite them. See ADR 0008.
/// </summary>
public enum ObservancePrecedence
{
    /// <summary>
    /// Rubric-sanctioned — either the sole valid observance, or one of several the rubric
    /// explicitly grants as equally valid (e.g. a Holy Day colliding with an ordinary
    /// Sunday, which BCP 2019 p.689 permits observing either way).
    /// </summary>
    Prescribed,

    /// <summary>
    /// Not sanctioned by any rubric, but a real deviation some churches practice anyway
    /// (e.g. skipping a Red-Letter Holy Day for the ordinary weekday reading instead).
    /// Surfaced rather than hidden, so the officiant can make an informed choice.
    /// </summary>
    CommonPractice,

    /// <summary>
    /// Rubric-sanctioned and has real propers, but never competes with or displaces the
    /// day's own Sunday/season observance — additive rather than ranked against it. Currently
    /// used only by the six BCP 2019 civil National Days (Thanksgiving Day, Canada Day,
    /// Independence Day, Remembrance Day, Memorial Day), which the rubric lists with proper
    /// lessons but states no precedence rule for. Deliberately not named <c>Optional</c> to
    /// avoid confusion with the unrelated <see cref="Feasts.FeastRank.Optional"/>. See ADR 0015.
    /// </summary>
    Supplementary,
}
