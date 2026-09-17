namespace FiveTalents.Calendar.Calendar;

/// <summary>
/// The kind of occurrence a day carries, in the order <see cref="LiturgicalDay.Occurrences"/>
/// is returned. A new, purpose-built ordering — not
/// <see cref="FiveTalents.Calendar.Feasts.FeastRank"/>'s ordinals, which rank named-Feast
/// competition on an orthogonal axis and actually invert part of this order for the lower
/// tiers (e.g. Rogation Day's <c>Minor</c> outranked Anglican/Ecumenical commemorations
/// there, the reverse of this order). See ADR 0016.
/// </summary>
public enum OccurrenceType
{
    PrincipalFeast,
    MajorFeast,
    Sunday,
    SeasonDay,
    AnglicanCommemoration,
    EcumenicalCommemoration,
    NationalDay,
    EmberDay,
    RogationDay,
    Antiphon,
}
