using System.Text.Json;

using FiveTalents.Calendar.Lectionary;

namespace FiveTalents.Calendar.Tests.Unit.Lectionary;

/// <summary>
/// Structural invariants over sunday-lectionary.json: completeness and well-formedness,
/// independent of citation correctness against the BCP (see LectionaryTests.cs for that).
/// Walks the JSON directly rather than through AcnaBcp2019Calendar/AcnaSundayLectionary, so
/// these catch data-shape problems the calendar-resolution logic never surfaces — e.g. a
/// Proper missing a reading type in Year B doesn't break any single spot-checked date test.
/// </summary>
public sealed class SundayLectionaryStructuralInvariantTests
{
    private static readonly string[] _canonicalFourTypes = ["FirstLesson", "Psalm", "SecondLesson", "Gospel"];

    // GreatVigil legitimately repeats FirstLesson 12 times (the Vigil's OT lessons) in a
    // flat, unstructured service — a known gap tracked by issue #12 (needs a proper
    // Service-of-Lessons model), not a transcription error this suite should flag.
    private static readonly HashSet<string> _knownDuplicateReadingTypeKeys = new(StringComparer.Ordinal)
    {
        "GreatVigil",
    };

    private static readonly JsonDocument _doc =
        LectionaryJsonInvariants.LoadEmbeddedResource(typeof(ReadingType).Assembly, "sunday-lectionary.json");

    // ── Every Proper has all four reading types, for all three years ──────────

    public static IEnumerable<object[]> ProperNumbers() =>
        Enumerable.Range(1, 29).Select(n => new object[] { n });

    [Theory]
    [MemberData(nameof(ProperNumbers))]
    public void Proper_HasAllFourReadingTypes_ForAllThreeYears(int properNumber)
    {
        string key = $"Proper{properNumber}";
        Assert.True(_doc.RootElement.TryGetProperty(key, out var properEl), $"Missing key '{key}'");

        foreach (string year in new[] { "A", "B", "C" })
        {
            Assert.True(properEl.TryGetProperty(year, out var yearEl), $"'{key}' is missing year '{year}'");

            var types = yearEl.EnumerateArray().Select(r => r.GetProperty("type").GetString()).Order();
            Assert.Equal(_canonicalFourTypes.Order(), types);
        }
    }

    // ── Every Holy Day has a complete reading set ──────────────────────────────

    public static IEnumerable<object[]> HolyDayKeys() =>
        LectionaryJsonInvariants.GetYearKeyedKeys(_doc)
            .Concat(_doc.RootElement.EnumerateObject()
                .Where(p => !p.Name.StartsWith('_') && p.Value.ValueKind == JsonValueKind.Array)
                .Select(p => p.Name))
            .Where(k => k.StartsWith("HolyDay_", StringComparison.Ordinal))
            .Select(k => new object[] { k });

    [Theory]
    [MemberData(nameof(HolyDayKeys))]
    public void HolyDay_HasCompleteReadingSet(string key)
    {
        var element = _doc.RootElement.GetProperty(key);
        var types = element.EnumerateArray().Select(r => r.GetProperty("type").GetString()).Order();
        Assert.Equal(_canonicalFourTypes.Order(), types);
    }

    // ── Every year-keyed entry is internally consistent across A/B/C ──────────
    // (A different, smaller reading set is legitimate — e.g. PalmSundayPalms is Gospel +
    // Psalm only — but it must be the *same* set in every year; a year silently missing a
    // reading type that its siblings have is a transcription gap, not a BCP variation.)

    public static IEnumerable<object[]> YearKeyedKeys() =>
        LectionaryJsonInvariants.GetYearKeyedKeys(_doc).Select(k => new object[] { k });

    [Theory]
    [MemberData(nameof(YearKeyedKeys))]
    public void YearKeyedEntry_HasConsistentReadingTypeComposition_AcrossAllThreeYears(string key)
    {
        var element = _doc.RootElement.GetProperty(key);

        Dictionary<string, List<string?>> compositionsByYear = element.EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.EnumerateArray().Select(r => r.GetProperty("type").GetString()).Order().ToList());

        var first = compositionsByYear.First();
        foreach (var (year, types) in compositionsByYear.Skip(1))
        {
            Assert.True(
                first.Value.SequenceEqual(types),
                $"'{key}' year '{year}' has reading types [{string.Join(", ", types)}] but year '{first.Key}' has [{string.Join(", ", first.Value)}]");
        }
    }

    // ── No reading type repeats within a single service, except the known GreatVigil gap ──

    [Fact]
    public void ReadingTypes_DoNotRepeatWithinAService_ExceptKnownGaps()
    {
        List<string> violations = LectionaryJsonInvariants.GetReadingGroups(_doc)
            .Where(g => !_knownDuplicateReadingTypeKeys.Contains(g.Key.Key))
            .SelectMany(g =>
            {
                var duplicateTypes = g.Select(e => e.Reading.GetProperty("type").GetString())
                    .GroupBy(t => t)
                    .Where(t => t.Count() > 1)
                    .Select(t => t.Key);

                string label = g.Key.Year is null ? g.Key.Key : $"{g.Key.Key}[{g.Key.Year}]";
                return duplicateTypes.Select(t => $"{label}: reading type '{t}' appears more than once");
            })
            .ToList();

        Assert.Empty(violations);
    }

    // ── Whole-file invariants shared with the future Daily Office JSON ────────

    [Fact]
    public void EveryReading_HasAValidReadingType()
    {
        List<string> violations = LectionaryJsonInvariants.GetAllReadings(_doc)
            .SelectMany(LectionaryJsonInvariants.ValidateReadingType)
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void EveryCitation_LooksLikeAScriptureReferenceOrKnownCanticle()
    {
        List<string> violations = LectionaryJsonInvariants.GetAllReadings(_doc)
            .SelectMany(LectionaryJsonInvariants.ValidateCitations)
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void EveryTranslationCode_IsNullOrNcp()
    {
        List<string> violations = LectionaryJsonInvariants.GetAllReadings(_doc)
            .SelectMany(LectionaryJsonInvariants.ValidateTranslationCode)
            .ToList();

        Assert.Empty(violations);
    }
}
