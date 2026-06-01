using System.Reflection;
using System.Text.Json;

using FiveTalents.Calendar.Calendar;
using FiveTalents.Calendar.Seasons;

namespace FiveTalents.Calendar.Lectionary;

/// <summary>
/// Provides Sunday, Holy Day, and Commemoration lectionary readings for the
/// ACNA BCP 2019, loaded from the embedded sunday-lectionary.json resource.
/// </summary>
internal static class AcnaSundayLectionary
{
    // Loaded once; key = occasion key, value = year map ("A"/"B"/"C" or flat array)
    private static readonly Dictionary<string, JsonElement> _data = Load();

    private static Dictionary<string, JsonElement> Load()
    {
        Assembly asm = Assembly.GetExecutingAssembly();
        string name = asm.GetManifestResourceNames()
            .First(n => n.EndsWith("sunday-lectionary.json", StringComparison.OrdinalIgnoreCase));

        using var stream = asm.GetManifestResourceStream(name)!;
        JsonDocument doc = JsonDocument.Parse(stream);
        return doc.RootElement.EnumerateObject()
            .Where(p => !p.Name.StartsWith('_'))
            .ToDictionary(p => p.Name, p => p.Value.Clone());
    }

    // ── Feast name → Holy Day JSON key ───────────────────────────────────────

    private static readonly Dictionary<string, string> _feastKeyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Andrew the Apostle"] = "HolyDay_Andrew",
        ["Thomas the Apostle"] = "HolyDay_Thomas",
        ["Stephen, Deacon and Martyr"] = "HolyDay_Stephen",
        ["John, Apostle and Evangelist"] = "HolyDay_John",
        ["The Holy Innocents"] = "HolyDay_HolyInnocents",
        ["The Circumcision and Holy Name of Our Lord Jesus Christ"] = "CircumcisionHolyName",
        ["Confession of Peter the Apostle"] = "HolyDay_ConfessionOfPeter",
        ["Conversion of Paul the Apostle"] = "HolyDay_ConversionOfPaul",
        ["The Presentation of Our Lord Jesus Christ in the Temple"] = "HolyDay_Presentation",
        ["Matthias the Apostle"] = "HolyDay_Matthias",
        ["Joseph, the Guardian of Jesus"] = "HolyDay_Joseph",
        ["The Annunciation of Our Lord Jesus Christ to the Virgin Mary"] = "HolyDay_Annunciation",
        ["Mark the Evangelist"] = "HolyDay_Mark",
        ["Philip and James, Apostles"] = "HolyDay_PhilipAndJames",
        ["The Visitation of the Virgin Mary to Elizabeth and Zechariah"] = "HolyDay_Visitation",
        ["Barnabas the Apostle"] = "HolyDay_Barnabas",
        ["The Nativity of John the Baptist"] = "HolyDay_NativityJohnBaptist",
        ["Peter and Paul, Apostles"] = "HolyDay_PeterAndPaul",
        ["Mary Magdalene"] = "HolyDay_MaryMagdalene",
        ["James the Elder, Apostle"] = "HolyDay_James",
        ["The Transfiguration of Our Lord Jesus Christ"] = "HolyDay_Transfiguration",
        ["The Virgin Mary, Mother of Our Lord Jesus Christ"] = "HolyDay_VirginMary",
        ["Bartholomew the Apostle"] = "HolyDay_Bartholomew",
        ["Holy Cross Day"] = "HolyDay_HolyCrossDay",
        ["Matthew, Apostle and Evangelist"] = "HolyDay_Matthew",
        ["Holy Michael and All Angels"] = "HolyDay_MichaelAllAngels",
        ["Luke the Evangelist and Companion of Paul"] = "HolyDay_Luke",
        ["James of Jerusalem, Bishop and Martyr, Brother of Our Lord"] = "HolyDay_JamesOfJerusalem",
        ["Simon and Jude, Apostles"] = "HolyDay_SimonAndJude",
        ["All Saints' Day"] = "HolyDay_AllSaints",
        // Moveable feast names that have entries in the JSON
        ["Ash Wednesday"] = "AshWednesday",
        ["Palm Sunday"] = "PalmSunday",
        ["Maundy Thursday"] = "MaundyThursday",
        ["Good Friday"] = "GoodFriday",
        ["Holy Saturday"] = "HolySaturday",
        ["Easter Day"] = "EasterPrincipalService",
        ["Ascension Day"] = "AscensionDay",
        ["The Day of Pentecost"] = "Pentecost",
        ["Trinity Sunday"] = "TrinitySunday",
    };

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the lectionary readings for the given day, or an empty list if
    /// no readings are assigned (e.g. an ordinary weekday with no proper).
    /// </summary>
    public static IReadOnlyList<LectionaryReading> GetReadings(LiturgicalDay day)
    {
        string? key = ResolveKey(day);
        if (key is null || !_data.TryGetValue(key, out var element))
        {
            return [];
        }

        return ParseReadings(element, day.Week.LectionaryYear);
    }

    // ── Key resolution ────────────────────────────────────────────────────────

    private static string? ResolveKey(LiturgicalDay day)
    {
        // 1. Holy Days take precedence, EXCEPT on Sundays in Advent, Lent, or Easter,
        //    where the Sunday proper is retained and the feast is transferred to a weekday.
        bool feastTransferred =
            day.Date.DayOfWeek == DayOfWeek.Sunday &&
            day.Season is LiturgicalSeason.Advent or LiturgicalSeason.Lent or LiturgicalSeason.Easter;

        if (!feastTransferred && day.Feast is not null
            && _feastKeyMap.TryGetValue(day.Feast.Name, out string? holyDayKey))
        {
            return holyDayKey;
        }

        // 2. Season-based lookup
        return day.Season switch
        {
            LiturgicalSeason.Advent => AdventKey(day),
            LiturgicalSeason.Christmas => ChristmasKey(day),
            LiturgicalSeason.Epiphany => EpiphanyKey(day),
            LiturgicalSeason.Lent => LentKey(day),
            LiturgicalSeason.HolyWeek => HolyWeekKey(day),
            LiturgicalSeason.Easter => EasterKey(day),
            LiturgicalSeason.Pentecost => "Pentecost",
            LiturgicalSeason.OrdinaryTime => OrdinaryTimeKey(day),
            _ => null,
        };
    }

    private static string? AdventKey(LiturgicalDay day) =>
        day.Week.WeekNumber switch { 1 => "Advent1", 2 => "Advent2", 3 => "Advent3", 4 => "Advent4", _ => null };

    private static string? ChristmasKey(LiturgicalDay day)
    {
        // Christmas Day itself is handled via the feast name → CircumcisionHolyName
        // First and Second Sunday of Christmas
        return day.Week.WeekNumber switch { 1 => "Christmas1", 2 => "Christmas2", _ => null };
    }

    private static string? EpiphanyKey(LiturgicalDay day)
    {
        if (day.Week.WeekNumber == 0)
        {
            return null; // Days After Epiphany — no Sunday proper
        }

        return day.Week.WeekNumber switch
        {
            1 => "Epiphany1",
            2 => "Epiphany2",
            3 => "Epiphany3",
            4 => "Epiphany4",
            5 => "Epiphany5",
            6 => "Epiphany6",
            7 => "Epiphany7",
            8 => "Epiphany8",
            _ => null, // Will be overridden by EpiphanySecondToLast/Last detection below
        };
    }

    private static string? LentKey(LiturgicalDay day)
    {
        if (day.Week.WeekNumber == 0)
        {
            return null; // Ash Wednesday → handled via feast name
        }

        return day.Week.WeekNumber switch { 1 => "Lent1", 2 => "Lent2", 3 => "Lent3", 4 => "Lent4", 5 => "Lent5", _ => null };
    }

    private static string? HolyWeekKey(LiturgicalDay day) =>
        day.Date.DayOfWeek switch
        {
            DayOfWeek.Sunday => "PalmSunday",
            DayOfWeek.Monday => "MondayHolyWeek",
            DayOfWeek.Tuesday => "TuesdayHolyWeek",
            DayOfWeek.Wednesday => "WednesdayHolyWeek",
            DayOfWeek.Thursday => "MaundyThursday",
            DayOfWeek.Friday => "GoodFriday",
            DayOfWeek.Saturday => "HolySaturday",
            _ => null,
        };

    private static string? EasterKey(LiturgicalDay day)
    {
        // Easter week weekdays
        if (day.Date.DayOfWeek != DayOfWeek.Sunday)
        {
            return day.Date.DayOfWeek switch
            {
                DayOfWeek.Monday => "EasterMonday",
                DayOfWeek.Tuesday => "EasterTuesday",
                DayOfWeek.Wednesday => "EasterWednesday",
                DayOfWeek.Thursday => "EasterThursday",
                DayOfWeek.Friday => "EasterFriday",
                DayOfWeek.Saturday => "EasterSaturday",
                _ => null,
            };
        }

        return day.Week.WeekNumber switch
        {
            1 => "EasterPrincipalService",
            2 => "Easter2",
            3 => "Easter3",
            4 => "Easter4",
            5 => "Easter5",
            6 => "Easter6",
            7 => "Easter7",
            _ => null,
        };
    }

    private static string? OrdinaryTimeKey(LiturgicalDay day)
    {
        if (day.ProperNumber is null)
        {
            return null;
        }

        if (day.Week.WeekNumber == 0)
        {
            return null; // Mon–Sat between Pentecost and Trinity
        }

        // Trinity Sunday (week 1 of OrdinaryTime)
        if (day.Date.DayOfWeek == DayOfWeek.Sunday && day.Week.WeekNumber == 1)
        {
            return "TrinitySunday";
        }

        // All Saints' Day (handled via feast name above, but included here for weekday reference)
        if (day.ProperNumber is >= 1 and <= 29)
        {
            return $"Proper{day.ProperNumber}";
        }

        return null;
    }

    // ── JSON parsing ──────────────────────────────────────────────────────────

    private static IReadOnlyList<LectionaryReading> ParseReadings(JsonElement element, char lectionaryYear)
    {
        // Year-keyed object: { "A": [...], "B": [...], "C": [...] }
        if (element.ValueKind == JsonValueKind.Object)
        {
            // Try the specific year first, then fall back (shouldn't be needed but defensive)
            if (element.TryGetProperty(lectionaryYear.ToString(), out var yearEl))
            {
                return ParseArray(yearEl);
            }

            return [];
        }

        // Flat array: year-independent readings
        if (element.ValueKind == JsonValueKind.Array)
        {
            return ParseArray(element);
        }

        return [];
    }

    private static IReadOnlyList<LectionaryReading> ParseArray(JsonElement array)
    {
        List<LectionaryReading> readings = new List<LectionaryReading>();
        foreach (var item in array.EnumerateArray())
        {
            if (!item.TryGetProperty("type", out var typeEl) ||
                !item.TryGetProperty("citation", out var citEl))
            {
                continue;
            }

            if (!Enum.TryParse<ReadingType>(typeEl.GetString(), out var type))
            {
                continue;
            }

            string? alternate = item.TryGetProperty("alternate", out var altEl)
                ? altEl.GetString()
                : null;

            readings.Add(new LectionaryReading
            {
                Type = type,
                Citation = citEl.GetString()!,
                AlternateCitation = alternate,
            });
        }
        return readings;
    }
}
