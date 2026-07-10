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
}
