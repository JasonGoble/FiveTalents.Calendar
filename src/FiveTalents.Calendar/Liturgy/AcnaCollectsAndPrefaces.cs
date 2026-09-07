using System.Reflection;
using System.Text.Json;

namespace FiveTalents.Calendar.Liturgy;

/// <summary>
/// Provides Collects and Preface names for the ACNA BCP 2019, loaded from the embedded
/// collects-prefaces.json resource. Keyed by the same occasion-key vocabulary as
/// <see cref="Lectionary.AcnaSundayLectionary"/> (e.g. "Advent1", "HolyDay_Andrew"), but
/// sourced separately since it comes from different BCP documents (Collects of the
/// Christian Year, Proper Prefaces) with only partial coverage so far. See ADR 0014.
/// </summary>
internal static class AcnaCollectsAndPrefaces
{
    private static readonly (Dictionary<string, JsonElement> Entries, Dictionary<string, string> PrefaceCatalog) _data = Load();

    private static (Dictionary<string, JsonElement>, Dictionary<string, string>) Load()
    {
        Assembly asm = Assembly.GetExecutingAssembly();
        string name = asm.GetManifestResourceNames()
            .First(n => n.EndsWith("collects-prefaces.json", StringComparison.OrdinalIgnoreCase));

        using var stream = asm.GetManifestResourceStream(name)!;
        JsonDocument doc = JsonDocument.Parse(stream);

        Dictionary<string, JsonElement> entries = doc.RootElement.GetProperty("entries").EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.Clone());

        Dictionary<string, string> prefaceCatalog = doc.RootElement.GetProperty("prefaceCatalog").EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.GetString()!);

        return (entries, prefaceCatalog);
    }

    /// <summary>The proper Collect for <paramref name="key"/>, or null when unsourced.</summary>
    public static Collect? TryGetCollect(string? key)
    {
        if (key is null || !_data.Entries.TryGetValue(key, out var entry) || !entry.TryGetProperty("collect", out var collectEl))
        {
            return null;
        }

        IReadOnlyList<string> alternateTexts = [];
        if (collectEl.TryGetProperty("alternateTexts", out var altEl))
        {
            alternateTexts = altEl.EnumerateArray().Select(e => e.GetString()!).ToList();
        }

        return new Collect
        {
            Text = collectEl.GetProperty("text").GetString()!,
            AlternateTexts = alternateTexts,
        };
    }

    /// <summary>The Preface catalog names for <paramref name="key"/>, or empty when unsourced.</summary>
    public static IReadOnlyList<string> GetPrefaceNames(string? key)
    {
        if (key is null || !_data.Entries.TryGetValue(key, out var entry) || !entry.TryGetProperty("prefaces", out var prefacesEl))
        {
            return [];
        }

        return prefacesEl.EnumerateArray().Select(e => e.GetString()!).ToList();
    }

    /// <summary>The full text of the named Preface, or null if the name isn't in the catalog.</summary>
    public static string? TryGetPrefaceText(string name) =>
        _data.PrefaceCatalog.TryGetValue(name, out string? text) ? text : null;
}
