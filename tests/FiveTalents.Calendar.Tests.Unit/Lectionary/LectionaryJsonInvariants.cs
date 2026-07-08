using System.Reflection;
using System.Text.Json;

using FiveTalents.Calendar.Lectionary;

namespace FiveTalents.Calendar.Tests.Unit.Lectionary;

/// <summary>
/// Structural invariant checks over an embedded lectionary JSON resource — completeness
/// and well-formedness, not citation correctness against the BCP. Shared between
/// sunday-lectionary.json today and daily-office-lectionary.json once issue #9 lands;
/// nothing here is specific to either file's key names.
/// </summary>
internal static class LectionaryJsonInvariants
{
    /// <summary>
    /// Alternate-citation values that are legitimate canticle names rather than scripture
    /// references, and so are exempt from the "looks like a reference" format check.
    /// </summary>
    private static readonly HashSet<string> _canticleNames = new(StringComparer.Ordinal)
    {
        "Magnificat",
        "Ecce Deus",
    };

    public readonly record struct ReadingEntry(string Key, string? Year, JsonElement Reading)
    {
        public string Label => Year is null ? Key : $"{Key}[{Year}]";
    }

    public static JsonDocument LoadEmbeddedResource(Assembly assembly, string resourceNameSuffix)
    {
        string name = assembly.GetManifestResourceNames()
            .First(n => n.EndsWith(resourceNameSuffix, StringComparison.OrdinalIgnoreCase));
        using var stream = assembly.GetManifestResourceStream(name)!;
        return JsonDocument.Parse(stream);
    }

    /// <summary>
    /// Returns every top-level key that is a year-keyed object: {"A": [...], "B": [...], "C": [...]}.
    /// </summary>
    public static IEnumerable<string> GetYearKeyedKeys(JsonDocument doc) =>
        doc.RootElement.EnumerateObject()
            .Where(p => !p.Name.StartsWith('_') && p.Value.ValueKind == JsonValueKind.Object)
            .Select(p => p.Name);

    /// <summary>
    /// Walks every reading in every top-level key (skipping "_"-prefixed metadata keys),
    /// handling both year-keyed objects and flat, year-independent arrays.
    /// </summary>
    public static IEnumerable<ReadingEntry> GetAllReadings(JsonDocument doc)
    {
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Name.StartsWith('_'))
            {
                continue;
            }

            if (prop.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var yearProp in prop.Value.EnumerateObject())
                {
                    foreach (var reading in yearProp.Value.EnumerateArray())
                    {
                        yield return new ReadingEntry(prop.Name, yearProp.Name, reading);
                    }
                }
            }
            else if (prop.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var reading in prop.Value.EnumerateArray())
                {
                    yield return new ReadingEntry(prop.Name, null, reading);
                }
            }
        }
    }

    /// <summary>
    /// Groups readings by (Key, Year) — one group per printed service/occasion — for checks
    /// that need to reason about a whole reading set rather than one reading at a time.
    /// </summary>
    public static IEnumerable<IGrouping<(string Key, string? Year), ReadingEntry>> GetReadingGroups(JsonDocument doc) =>
        GetAllReadings(doc).GroupBy(e => (e.Key, e.Year));

    // ── Per-reading validation ───────────────────────────────────────────────

    public static IEnumerable<string> ValidateReadingType(ReadingEntry entry)
    {
        if (!entry.Reading.TryGetProperty("type", out var typeEl) ||
            !Enum.TryParse<ReadingType>(typeEl.GetString(), out _))
        {
            yield return $"{entry.Label}: invalid or missing 'type' ({typeEl.GetString() ?? "null"})";
        }
    }

    public static IEnumerable<string> ValidateCitations(ReadingEntry entry)
    {
        string citation = entry.Reading.GetProperty("citation").GetString() ?? "";

        foreach (string msg in ValidateCitationText(citation, entry.Label, "citation", allowCanticle: false))
        {
            yield return msg;
        }

        List<string> alternates = GetAlternates(entry.Reading).ToList();
        foreach (string alt in alternates)
        {
            foreach (string msg in ValidateCitationText(alt, entry.Label, "alternate citation", allowCanticle: true))
            {
                yield return msg;
            }

            if (alt == citation)
            {
                yield return $"{entry.Label}: alternate citation '{alt}' duplicates the primary citation";
            }
        }

        if (alternates.Count != alternates.Distinct(StringComparer.Ordinal).Count())
        {
            yield return $"{entry.Label}: alternate citations contain internal duplicates ({string.Join(", ", alternates)})";
        }
    }

    public static IEnumerable<string> ValidateTranslationCode(ReadingEntry entry)
    {
        if (entry.Reading.TryGetProperty("translationCode", out var tcEl) && tcEl.ValueKind != JsonValueKind.Null)
        {
            string? code = tcEl.GetString();
            if (code != "NCP")
            {
                yield return $"{entry.Label}: unexpected translationCode '{code}' (expected null or \"NCP\")";
            }
        }
    }

    private static IEnumerable<string> ValidateCitationText(string text, string label, string field, bool allowCanticle)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield return $"{label}: {field} is empty or whitespace";
            yield break;
        }

        if (text != text.Trim())
        {
            yield return $"{label}: {field} '{text}' has leading/trailing whitespace";
        }

        if (text.Contains("  ", StringComparison.Ordinal))
        {
            yield return $"{label}: {field} '{text}' contains a double space";
        }

        bool looksLikeReference = text.Any(char.IsDigit);
        bool isKnownCanticle = allowCanticle && _canticleNames.Contains(text);
        if (!looksLikeReference && !isKnownCanticle)
        {
            yield return $"{label}: {field} '{text}' doesn't look like a scripture reference (no digit) and isn't a recognized canticle name";
        }
    }

    private static IEnumerable<string> GetAlternates(JsonElement reading)
    {
        if (!reading.TryGetProperty("alternate", out var altEl) || altEl.ValueKind == JsonValueKind.Null)
        {
            yield break;
        }

        if (altEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var e in altEl.EnumerateArray())
            {
                yield return e.GetString()!;
            }
        }
        else
        {
            yield return altEl.GetString()!;
        }
    }
}
