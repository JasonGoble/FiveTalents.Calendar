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
    /// Returns the liturgical services for the given day, each containing their own
    /// readings. Returns an empty list when no readings are assigned (e.g. an ordinary
    /// weekday with no proper). Most days return a single unnamed service; Palm Sunday
    /// returns two named services ("Liturgy of the Palms" and "Liturgy of the Word").
    /// </summary>
    /// <remarks>
    /// Prefer <see cref="AcnaBcp2019Calendar.GetPossibleEucharistObservances"/>
    /// for anything precedence-aware — this is now a thin convenience wrapper around
    /// whichever key <paramref name="key"/> resolves to, with no Feast-vs-season decision
    /// of its own. See ADR 0008.
    /// </remarks>
    public static IReadOnlyList<LiturgicalService> BuildServicesForKey(string? key, char lectionaryYear)
    {
        if (key is null)
        {
            return [];
        }

        if (key == "PalmSunday")
        {
            var palms = BuildService("PalmSundayPalms", lectionaryYear);
            var word = BuildService("PalmSunday", lectionaryYear);
            return
            [
                new LiturgicalService { Name = "Liturgy of the Palms", Readings = palms },
                new LiturgicalService { Name = "Liturgy of the Word", Readings = word },
            ];
        }

        if (!_data.TryGetValue(key, out var element))
        {
            return [];
        }

        var readings = ParseReadings(element, lectionaryYear);
        return readings.Count == 0 ? [] : [new LiturgicalService { Readings = readings }];
    }

    /// <summary>The JSON key for a named Feast's own propers, or null if it has none of its own.</summary>
    public static string? TryGetFeastKey(FeastDay feast) =>
        _feastKeyMap.TryGetValue(feast.Name, out string? key) ? key : null;

    /// <summary>
    /// The JSON key for <paramref name="date"/>'s season/Proper propers, ignoring any Feast
    /// entirely — i.e. what this date would show if no Holy Day were in play.
    /// </summary>
    public static string? GetSeasonKey(DateOnly date, LiturgicalSeason season, int weekNumber, int? properNumber) =>
        season switch
        {
            LiturgicalSeason.Advent => AdventKey(weekNumber),
            LiturgicalSeason.Christmas => ChristmasKey(weekNumber),
            LiturgicalSeason.Epiphany => EpiphanyKey(date, weekNumber),
            LiturgicalSeason.Lent => LentKey(weekNumber),
            LiturgicalSeason.HolyWeek => HolyWeekKey(date),
            LiturgicalSeason.Easter => EasterKey(date, weekNumber),
            LiturgicalSeason.Pentecost => "Pentecost",
            LiturgicalSeason.OrdinaryTime => OrdinaryTimeKey(date, weekNumber, properNumber),
            _ => null,
        };

    private static IReadOnlyList<LectionaryReading> BuildService(string key, char lectionaryYear)
    {
        if (!_data.TryGetValue(key, out var element))
        {
            return [];
        }

        return ParseReadings(element, lectionaryYear);
    }

    // ── Season key resolution ────────────────────────────────────────────────

    private static string? AdventKey(int weekNumber) =>
        weekNumber switch { 1 => "Advent1", 2 => "Advent2", 3 => "Advent3", 4 => "Advent4", _ => null };

    private static string? ChristmasKey(int weekNumber)
    {
        // Christmas Day itself is handled via the feast name → CircumcisionHolyName
        // First and Second Sunday of Christmas
        return weekNumber switch { 1 => "Christmas1", 2 => "Christmas2", _ => null };
    }

    private static string? EpiphanyKey(DateOnly date, int weekNumber)
    {
        if (weekNumber == 0)
        {
            return null; // Days After Epiphany — no Sunday proper
        }

        // The last one/two Sundays of Epiphany always use their own fixed propers
        // (Transfiguration and the Sunday before it), regardless of the forward-counted
        // week number — the number of Epiphany Sundays varies by year depending on when
        // Ash Wednesday falls, but these two are always Easter − 49 / − 56 days.
        var easter = EasterCalculator.GetEaster(date.Year);
        if (date == easter.AddDays(-49))
        {
            return "EpiphanyLast";
        }

        if (date == easter.AddDays(-56))
        {
            return "EpiphanySecondToLast";
        }

        return weekNumber switch
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

    private static string? LentKey(int weekNumber)
    {
        if (weekNumber == 0)
        {
            return null; // Ash Wednesday → handled via feast name
        }

        return weekNumber switch { 1 => "Lent1", 2 => "Lent2", 3 => "Lent3", 4 => "Lent4", 5 => "Lent5", _ => null };
    }

    private static string? HolyWeekKey(DateOnly date) =>
        date.DayOfWeek switch
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

    private static string? EasterKey(DateOnly date, int weekNumber)
    {
        // Easter week weekdays
        if (date.DayOfWeek != DayOfWeek.Sunday)
        {
            return date.DayOfWeek switch
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

        return weekNumber switch
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

    private static string? OrdinaryTimeKey(DateOnly date, int weekNumber, int? properNumber)
    {
        if (properNumber is null)
        {
            return null;
        }

        if (weekNumber == 0)
        {
            return null; // Mon–Sat between Pentecost and Trinity
        }

        // Trinity Sunday (week 1 of OrdinaryTime)
        if (date.DayOfWeek == DayOfWeek.Sunday && weekNumber == 1)
        {
            return "TrinitySunday";
        }

        // All Saints' Day (handled via feast name above, but included here for weekday reference)
        if (properNumber is >= 1 and <= 29)
        {
            return $"Proper{properNumber}";
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

            IReadOnlyList<string> alternates = [];
            if (item.TryGetProperty("alternate", out var altEl))
            {
                alternates = altEl.ValueKind == JsonValueKind.Array
                    ? altEl.EnumerateArray().Select(e => e.GetString()!).ToList()
                    : [altEl.GetString()!];
            }

            string? translationCode = item.TryGetProperty("translationCode", out var tcEl)
                ? tcEl.GetString()
                : null;

            readings.Add(new LectionaryReading
            {
                Type = type,
                Citation = citEl.GetString()!,
                AlternateCitations = alternates,
                TranslationCode = translationCode,
            });
        }
        return readings;
    }
}
