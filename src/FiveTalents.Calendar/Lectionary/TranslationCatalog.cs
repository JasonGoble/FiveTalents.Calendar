using System.Reflection;
using System.Text.Json;

namespace FiveTalents.Calendar.Lectionary;

/// <summary>
/// Reference catalog of translation/versification codes used by
/// <see cref="LectionaryReading.TranslationCode"/>, loaded from the embedded
/// translations.json resource. Tradition-agnostic — codes are shared across all traditions.
/// </summary>
public static class TranslationCatalog
{
    private static readonly IReadOnlyList<TranslationInfo> _translations = Load();

    /// <summary>Returns all known translation codes and their human-readable names.</summary>
    public static IReadOnlyList<TranslationInfo> GetAll() => _translations;

    private static IReadOnlyList<TranslationInfo> Load()
    {
        Assembly asm = Assembly.GetExecutingAssembly();
        string name = asm.GetManifestResourceNames()
            .First(n => n.EndsWith("translations.json", StringComparison.OrdinalIgnoreCase));

        using var stream = asm.GetManifestResourceStream(name)!;
        JsonDocument doc = JsonDocument.Parse(stream);
        JsonElement array = doc.RootElement.GetProperty("translations");

        List<TranslationInfo> result = new List<TranslationInfo>();
        foreach (var item in array.EnumerateArray())
        {
            IReadOnlyList<string> additionalBooks = item.TryGetProperty("additionalBooks", out var booksEl)
                ? booksEl.EnumerateArray().Select(b => b.GetString()!).ToList()
                : [];

            result.Add(new TranslationInfo
            {
                Code = item.GetProperty("code").GetString()!,
                Name = item.GetProperty("name").GetString()!,
                ResourceType = Enum.Parse<TranslationResourceType>(item.GetProperty("resourceType").GetString()!),
                AdditionalBooks = additionalBooks,
            });
        }

        return result;
    }
}
