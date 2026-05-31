namespace FiveTalents.Calendar.Calendar;

/// <summary>
/// The liturgical tradition / lectionary that governs calendar calculations.
/// </summary>
public enum LiturgicalTradition
{
    /// <summary>Anglican Church in North America — Book of Common Prayer 2019.</summary>
    AcnaBcp2019,

    /// <summary>The Revised Common Lectionary (ecumenical).</summary>
    RevisedCommonLectionary,

    /// <summary>The (original) Common Lectionary (1983).</summary>
    CommonLectionary,

    /// <summary>The Episcopal Church (TEC) lectionary.</summary>
    Episcopal,
}
