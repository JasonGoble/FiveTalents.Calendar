namespace FiveTalents.Calendar.Feasts;

/// <summary>Precedence ranking for liturgical observances (higher value = higher precedence).</summary>
public enum FeastRank
{
    Commemoration = 1,
    Optional = 2,
    Major = 4,
    Principal = 5,
}
