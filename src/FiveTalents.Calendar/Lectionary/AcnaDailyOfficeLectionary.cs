using System.Reflection;
using System.Text.Json;

using FiveTalents.Calendar.Calendar;

namespace FiveTalents.Calendar.Lectionary;

/// <summary>
/// Provides Morning and Evening Prayer readings for the ACNA BCP 2019, loaded from four
/// embedded resources: the calendar-date Lessons grid, the traditional 30-day Psalter
/// cycle, the alternate 60-day Psalter cycle (used only as a day-31 fallback — see
/// issue #33 for exposing it as a real choice), and the Easter-relative movable Holy Days.
/// See docs/decisions/0005-daily-office-readings-model.md for the design rationale.
/// </summary>
internal static class AcnaDailyOfficeLectionary
{
    private static readonly Dictionary<string, JsonElement> _lessons =
        Load("acna-bcp2019-daily-office-lessons.json");

    private static readonly Dictionary<string, JsonElement> _psalter30Day =
        Load("acna-bcp2019-daily-office-psalter-30day.json");

    private static readonly Dictionary<string, JsonElement> _psalter60Day =
        Load("acna-bcp2019-daily-office-psalter-60day.json");

    private static readonly Dictionary<string, JsonElement> _movableHolyDays =
        Load("acna-bcp2019-daily-office-movable-holy-days.json");

    private static Dictionary<string, JsonElement> Load(string resourceFileName)
    {
        Assembly asm = Assembly.GetExecutingAssembly();
        string name = asm.GetManifestResourceNames()
            .First(n => n.EndsWith(resourceFileName, StringComparison.OrdinalIgnoreCase));

        using var stream = asm.GetManifestResourceStream(name)!;
        JsonDocument doc = JsonDocument.Parse(stream);
        return doc.RootElement.EnumerateObject()
            .Where(p => !p.Name.StartsWith('_'))
            .ToDictionary(p => p.Name, p => p.Value.Clone());
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the Morning and Evening Prayer readings for the given date. Always
    /// populated — unlike the Sunday/Holy Day Eucharist lectionary, the Daily Office
    /// is prayed every day of the year.
    /// </summary>
    public static DailyOfficeReadings GetReadings(DateOnly date)
    {
        DateOnly easter = EasterCalculator.GetEaster(date.Year);
        int offsetFromEaster = date.DayNumber - easter.DayNumber;

        foreach (var entry in _movableHolyDays.Values)
        {
            if (entry.GetProperty("easterOffsetDays").GetInt32() == offsetFromEaster)
            {
                return BuildFromMovableHolyDay(entry);
            }
        }

        return BuildFromCalendarGrid(date);
    }

    // ── Movable Holy Days (proper Lessons, no Year I/II division) ───────────────

    private static DailyOfficeReadings BuildFromMovableHolyDay(JsonElement entry)
    {
        var mp = entry.GetProperty("morningPrayer");
        var ep = entry.GetProperty("eveningPrayer");

        return new DailyOfficeReadings
        {
            MorningPrayer = new LiturgicalService
            {
                Name = "Morning Prayer",
                Readings =
                [
                    new LectionaryReading { Type = ReadingType.FirstLesson, Citation = mp.GetProperty("firstLesson").GetString()! },
                    new LectionaryReading { Type = ReadingType.SecondLesson, Citation = mp.GetProperty("secondLesson").GetString()! },
                    BuildPsalmReading(ReadPsalmTokens(mp)),
                ],
            },
            EveningPrayer = new LiturgicalService
            {
                Name = "Evening Prayer",
                Readings =
                [
                    new LectionaryReading { Type = ReadingType.FirstLesson, Citation = ep.GetProperty("firstLesson").GetString()! },
                    new LectionaryReading { Type = ReadingType.SecondLesson, Citation = ep.GetProperty("secondLesson").GetString()! },
                    BuildPsalmReading(ReadPsalmTokens(ep)),
                ],
            },
        };
    }

    // ── Regular calendar-date grid (Year I/II division) ──────────────────────────

    /// <summary>
    /// Odd calendar years (Year I) use the day's Morning-column Lesson pair; even
    /// calendar years (Year II) use the Evening-column pair. Whichever pair is active,
    /// its first lesson is read at Morning Prayer and its second at Evening Prayer.
    /// This is pure calendar-year parity — independent of <see cref="LiturgicalWeek.LectionaryYear"/>
    /// (the Sunday Eucharist's A/B/C cycle); the two must never be conflated.
    /// </summary>
    private static DailyOfficeReadings BuildFromCalendarGrid(DateOnly date)
    {
        string key = $"{date.Month:D2}-{date.Day:D2}";
        bool isYearI = date.Year % 2 != 0;

        string morningLessonType = isYearI ? "MorningFirstLesson" : "EveningFirstLesson";
        string eveningLessonType = isYearI ? "MorningSecondLesson" : "EveningSecondLesson";

        LectionaryReading? morningLesson = null;
        LectionaryReading? eveningLesson = null;

        if (_lessons.TryGetValue(key, out var lessonsElement))
        {
            foreach (var item in lessonsElement.EnumerateArray())
            {
                string type = item.GetProperty("type").GetString()!;
                string citation = item.GetProperty("citation").GetString()!;
                IReadOnlyList<string> alternates = ParseAlternates(item);

                if (type == morningLessonType)
                {
                    morningLesson = new LectionaryReading { Type = ReadingType.MorningPrayer, Citation = citation, AlternateCitations = alternates };
                }
                else if (type == eveningLessonType)
                {
                    eveningLesson = new LectionaryReading { Type = ReadingType.EveningPrayer, Citation = citation, AlternateCitations = alternates };
                }
            }
        }

        List<LectionaryReading> morningReadings = [];
        if (morningLesson is not null)
        {
            morningReadings.Add(morningLesson);
        }
        morningReadings.Add(BuildPsalmReading(ReadPsalmTokensForGridOffice(date, key, "morningPrayer")));

        List<LectionaryReading> eveningReadings = [];
        if (eveningLesson is not null)
        {
            eveningReadings.Add(eveningLesson);
        }
        eveningReadings.Add(BuildPsalmReading(ReadPsalmTokensForGridOffice(date, key, "eveningPrayer")));

        return new DailyOfficeReadings
        {
            MorningPrayer = new LiturgicalService { Name = "Morning Prayer", Readings = morningReadings },
            EveningPrayer = new LiturgicalService { Name = "Evening Prayer", Readings = eveningReadings },
        };
    }

    private static IReadOnlyList<string> ParseAlternates(JsonElement item)
    {
        if (!item.TryGetProperty("alternate", out var altEl))
        {
            return [];
        }

        return altEl.ValueKind == JsonValueKind.Array
            ? altEl.EnumerateArray().Select(e => e.GetString()!).ToList()
            : [altEl.GetString()!];
    }

    // ── Psalms ────────────────────────────────────────────────────────────────

    /// <summary>
    /// The 30-day cycle is the BCP's "traditional" cycle and is used as the sole Psalm
    /// citation, except on day 31 — the 30-day cycle has no fixed entry that day (the BCP
    /// grants free discretion; see issue #30), so the 60-day cycle is used as a fallback
    /// so a Psalm reading is never missing. Exposing the 60-day cycle as a real, chooseable
    /// alternative (not just this fallback) is tracked in issue #33.
    /// </summary>
    private static IReadOnlyList<string> ReadPsalmTokensForGridOffice(DateOnly date, string mmddKey, string officeProperty)
    {
        if (_psalter30Day.TryGetValue(date.Day.ToString(), out var dayElement))
        {
            return dayElement.GetProperty(officeProperty)
                .EnumerateArray()
                .Select(TokenToString)
                .ToList();
        }

        return _psalter60Day[mmddKey]
            .GetProperty(officeProperty)
            .GetProperty("psalms")
            .EnumerateArray()
            .Select(e => e.GetString()!)
            .ToList();
    }

    private static IReadOnlyList<string> ReadPsalmTokens(JsonElement office) =>
        office.GetProperty("psalms").EnumerateArray().Select(e => e.GetString()!).ToList();

    private static string TokenToString(JsonElement element) =>
        element.ValueKind == JsonValueKind.Number ? element.GetInt32().ToString() : element.GetString()!;

    /// <summary>
    /// Every Daily Office Psalm reading carries NCP versification, matching
    /// <see cref="AcnaSundayLectionary"/>'s Sunday-lectionary convention where all Psalm
    /// entries carry <c>TranslationCode: "NCP"</c> regardless of whether the individual
    /// citation happens to be a whole Psalm or a verse range.
    /// </summary>
    private static LectionaryReading BuildPsalmReading(IReadOnlyList<string> tokens) =>
        new LectionaryReading
        {
            Type = ReadingType.Psalm,
            Citation = FormatPsalmCitation(tokens),
            TranslationCode = "NCP",
        };

    private static string FormatPsalmCitation(IReadOnlyList<string> tokens) =>
        tokens.Count == 1 ? $"Ps {tokens[0]}" : $"Ps {string.Join(", ", tokens)}";
}
