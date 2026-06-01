namespace FiveTalents.Calendar.Feasts;

/// <summary>
/// Identifies which Common of Saints (BCP 2019 pp. 730-733) applies to a
/// commemoration or holy day when no specific lectionary readings are assigned.
/// Maps to the <c>Common_*</c> keys in the lectionary JSON.
/// </summary>
public enum CommemorationCommon
{
    Martyr,
    MissionaryEvangelist,
    Pastor,
    TeacherOfFaith,
    MonasticReligious,
    Ecumenist,
    ReformerOfChurch,
    RenewerOfSociety,
    AnyCommemorationI,
    AnyCommemorationII,
}
