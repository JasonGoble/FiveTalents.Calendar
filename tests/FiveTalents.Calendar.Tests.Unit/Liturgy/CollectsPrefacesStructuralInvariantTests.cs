using System.Text.Json;

using FiveTalents.Calendar.Liturgy;

using FiveTalents.Calendar.Tests.Unit.Lectionary;

namespace FiveTalents.Calendar.Tests.Unit.Liturgy;

/// <summary>
/// Structural invariants over collects-prefaces.json: every Preface name an entry references
/// must exist in the catalog, and every Collect must carry non-empty text. Independent of
/// whether the content is correct against the BCP (see AcnaCollectsAndPrefacesTests for
/// that) — mirrors SundayLectionaryStructuralInvariantTests' split of those two concerns.
/// </summary>
public sealed class CollectsPrefacesStructuralInvariantTests
{
    private static readonly JsonDocument _doc =
        LectionaryJsonInvariants.LoadEmbeddedResource(typeof(Collect).Assembly, "collects-prefaces.json");

    public static IEnumerable<object[]> EntryKeys() =>
        _doc.RootElement.GetProperty("entries").EnumerateObject().Select(p => new object[] { p.Name });

    [Theory]
    [MemberData(nameof(EntryKeys))]
    public void Entry_CollectHasNonEmptyText(string key)
    {
        var entry = _doc.RootElement.GetProperty("entries").GetProperty(key);

        Assert.True(entry.TryGetProperty("collect", out var collectEl), $"'{key}' has no 'collect'");
        Assert.False(string.IsNullOrWhiteSpace(collectEl.GetProperty("text").GetString()));
    }

    [Theory]
    [MemberData(nameof(EntryKeys))]
    public void Entry_PrefaceNames_AllExistInCatalog(string key)
    {
        var entry = _doc.RootElement.GetProperty("entries").GetProperty(key);
        var catalog = _doc.RootElement.GetProperty("prefaceCatalog");

        if (!entry.TryGetProperty("prefaces", out var prefacesEl))
        {
            return;
        }

        foreach (var nameEl in prefacesEl.EnumerateArray())
        {
            string name = nameEl.GetString()!;
            Assert.True(catalog.TryGetProperty(name, out _), $"'{key}' references unknown Preface '{name}'");
        }
    }

    [Fact]
    public void PrefaceCatalog_EveryEntryHasNonEmptyText()
    {
        foreach (var property in _doc.RootElement.GetProperty("prefaceCatalog").EnumerateObject())
        {
            Assert.False(string.IsNullOrWhiteSpace(property.Value.GetString()), $"Preface '{property.Name}' has empty text");
        }
    }
}
