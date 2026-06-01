using System.Reflection;
using System.Text.Json;

using FiveTalents.Calendar.Calendar;
using FiveTalents.Calendar.Feasts;
using FiveTalents.Calendar.Seasons;

namespace FiveTalents.Calendar.Lectionary;

/// <summary>
/// Provides Sunday, Holy Day, and Commemoration lectionary readings for the
/// ACNA BCP 2019, loaded from the embedded sunday-lectionary.json resource.
/// </summary>
internal static class AcnaSundayLectionary
{
    // Loaded once; key = occasion key, value = year map ("A"/"B"/"C" or array)
    private static readonly Dictionary<string, JsonElement> _data = Load();

    private static Dictionary<string, JsonElement> Load()
    {
        Assembly asm = Assembly.GetExecutingAssembly();
        string name = asm.GetManifestResourceNames()
            .First(n => n.EndsWith("acna-bcp2019-lectionary.json", StringComparison.OrdinalIgnoreCase));

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
        // Fixed feasts
        ["Christmas Day"] = "ChristmasDay",
        // Easter week weekdays
        ["Monday in Easter Week"] = "EasterMonday",
        ["Tuesday in Easter Week"] = "EasterTuesday",
        ["Wednesday in Easter Week"] = "EasterWednesday",
        ["Thursday in Easter Week"] = "EasterThursday",
        ["Friday in Easter Week"] = "EasterFriday",
        ["Saturday in Easter Week"] = "EasterSaturday",
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
    /// Returns the reading sets for a specific feast or commemoration. For feasts
    /// with entries in the lectionary, returns their specific readings. For
    /// commemorations, falls back to the appropriate Common of Saints.
    /// Returns an empty list when no readings can be resolved.
    /// </summary>
    public static IReadOnlyList<ReadingSet> GetReadingsForFeast(FeastDay feast, char lectionaryYear)
    {
        // Specific Holy Day propers
        if (_feastKeyMap.TryGetValue(feast.Name, out string? key)
            && _data.TryGetValue(key, out var element))
        {
            return ParseReadings(element, lectionaryYear);
        }

        // Common of Saints fallback for commemorations
        if (feast.Common is CommemorationCommon common)
        {
            string commonKey = $"Common_{common}";
            if (_data.TryGetValue(commonKey, out var commonEl))
            {
                return ParseReadings(commonEl, lectionaryYear);
            }
        }

        return [];
    }

    /// <summary>
    /// Returns the season proper reading sets for the given day (ignoring any feast).
    /// Used for the season Sunday proper observance, and for transferred feast days.
    /// Returns an empty list when no season proper applies.
    /// </summary>
    public static IReadOnlyList<ReadingSet> GetSeasonProperReadings(LiturgicalDay day)
    {
        string? key = ResolveSeasonKey(day);
        if (key is null || !_data.TryGetValue(key, out var element))
        {
            return [];
        }

        return ParseReadings(element, day.Week.LectionaryYear);
    }

    /// <summary>
    /// Returns the human-readable name for the season Sunday proper on the given day.
    /// Returns null when the day has no season proper (e.g. a weekday, or Days After Epiphany).
    /// </summary>
    public static string? GetSeasonProperName(LiturgicalDay day)
    {
        if (day.Date.DayOfWeek != DayOfWeek.Sunday)
        {
            return null;
        }

        return day.Season switch
        {
            LiturgicalSeason.Advent => day.Week.WeekNumber switch
            {
                1 => "The First Sunday of Advent",
                2 => "The Second Sunday of Advent",
                3 => "The Third Sunday of Advent",
                4 => "The Fourth Sunday of Advent",
                _ => null,
            },
            LiturgicalSeason.Christmas => day.Week.WeekNumber switch
            {
                1 => "The First Sunday after Christmas Day",
                2 => "The Second Sunday after Christmas Day",
                _ => null,
            },
            LiturgicalSeason.Epiphany => day.Week.WeekNumber switch
            {
                0 => null,
                1 => "The First Sunday after the Epiphany",
                2 => "The Second Sunday after the Epiphany",
                3 => "The Third Sunday after the Epiphany",
                4 => "The Fourth Sunday after the Epiphany",
                5 => "The Fifth Sunday after the Epiphany",
                6 => "The Sixth Sunday after the Epiphany",
                7 => "The Seventh Sunday after the Epiphany",
                8 => "The Eighth Sunday after the Epiphany",
                _ => null,
            },
            LiturgicalSeason.Lent => day.Week.WeekNumber switch
            {
                1 => "The First Sunday in Lent",
                2 => "The Second Sunday in Lent",
                3 => "The Third Sunday in Lent",
                4 => "The Fourth Sunday in Lent",
                5 => "The Fifth Sunday in Lent",
                _ => null,
            },
            LiturgicalSeason.Easter => day.Week.WeekNumber switch
            {
                1 => "The First Sunday of Easter",
                2 => "The Second Sunday of Easter",
                3 => "The Third Sunday of Easter",
                4 => "The Fourth Sunday of Easter",
                5 => "The Fifth Sunday of Easter",
                6 => "The Sixth Sunday of Easter",
                7 => "The Seventh Sunday of Easter",
                _ => null,
            },
            LiturgicalSeason.OrdinaryTime when day.ProperNumber is int p
                => $"Proper {p}",
            _ => null,
        };
    }

    // ── Season key resolution (no feast lookup) ───────────────────────────────

    private static string? ResolveSeasonKey(LiturgicalDay day)
    {
        return day.Season switch
        {
            LiturgicalSeason.Advent => AdventKey(day),
            LiturgicalSeason.Christmas => ChristmasKey(day),
            LiturgicalSeason.Epiphany => EpiphanyKey(day),
            LiturgicalSeason.Lent => LentKey(day),
            LiturgicalSeason.HolyWeek => HolyWeekKey(day),
            LiturgicalSeason.Easter => EasterKey(day),
            LiturgicalSeason.OrdinaryTime => OrdinaryTimeKey(day),
            _ => null,
        };
    }

    private static string? AdventKey(LiturgicalDay day) =>
        day.Week.WeekNumber switch { 1 => "Advent1", 2 => "Advent2", 3 => "Advent3", 4 => "Advent4", _ => null };

    private static string? ChristmasKey(LiturgicalDay day) =>
        day.Week.WeekNumber switch { 1 => "Christmas1", 2 => "Christmas2", _ => null };

    private static string? EpiphanyKey(LiturgicalDay day)
    {
        if (day.Week.WeekNumber == 0)
        {
            return null;
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
            _ => null,
        };
    }

    private static string? LentKey(LiturgicalDay day)
    {
        if (day.Week.WeekNumber == 0)
        {
            return null; // Ash Wednesday → handled as feast
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
        if (day.ProperNumber is null || day.Week.WeekNumber == 0)
        {
            return null;
        }

        if (day.Date.DayOfWeek == DayOfWeek.Sunday && day.Week.WeekNumber == 1)
        {
            return "TrinitySunday";
        }

        if (day.ProperNumber is >= 1 and <= 29)
        {
            return $"Proper{day.ProperNumber}";
        }

        return null;
    }

    // ── JSON parsing ──────────────────────────────────────────────────────────

    private static IReadOnlyList<ReadingSet> ParseReadings(JsonElement element, char lectionaryYear)
    {
        // Year-keyed object: { "A": [...], "B": [...], "C": [...] }
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty(lectionaryYear.ToString(), out var yearEl))
            {
                return [new ReadingSet { Readings = ParseArray(yearEl) }];
            }

            return [];
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            // Labeled multi-set format: [ { "label": "I", "readings": [...] }, ... ]
            var first = element.EnumerateArray().FirstOrDefault();
            if (first.ValueKind == JsonValueKind.Object && first.TryGetProperty("readings", out _))
            {
                return ParseLabeledSets(element);
            }

            // Flat array: single unnamed set, year-independent
            return [new ReadingSet { Readings = ParseArray(element) }];
        }

        return [];
    }

    private static IReadOnlyList<ReadingSet> ParseLabeledSets(JsonElement array)
    {
        List<ReadingSet> sets = new List<ReadingSet>();
        foreach (var item in array.EnumerateArray())
        {
            string? label = item.TryGetProperty("label", out var labelEl) ? labelEl.GetString() : null;
            IReadOnlyList<LectionaryReading> readings = item.TryGetProperty("readings", out var readingsEl)
                ? ParseArray(readingsEl)
                : [];
            sets.Add(new ReadingSet { Label = label, Readings = readings });
        }
        return sets;
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
