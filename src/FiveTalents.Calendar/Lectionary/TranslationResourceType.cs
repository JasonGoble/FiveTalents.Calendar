namespace FiveTalents.Calendar.Lectionary;

/// <summary>
/// What kind of resource a <see cref="TranslationInfo"/> entry represents — determines
/// which books it can apply to (a Psalter only ever covers Psalms, regardless of what
/// citations exist elsewhere).
/// </summary>
public enum TranslationResourceType
{
    Bible,
    Psalter,
}
