using FiveTalents.Calendar.Lectionary;

namespace FiveTalents.Calendar.Tests.Unit.Lectionary;

public sealed class TranslationCatalogTests
{
    [Fact]
    public void GetAll_ReturnsKnownCodes()
    {
        var translations = TranslationCatalog.GetAll();

        Assert.Contains(translations, t => t.Code == "ESV" && t.Name == "English Standard Version");
        Assert.Contains(translations, t => t.Code == "NCP" && t.Name == "New Coverdale Psalter");
    }

    [Fact]
    public void GetAll_Bibles_HaveBibleResourceType()
    {
        var translations = TranslationCatalog.GetAll();

        Assert.Equal(TranslationResourceType.Bible, translations.Single(t => t.Code == "ESV").ResourceType);
        Assert.Equal(TranslationResourceType.Bible, translations.Single(t => t.Code == "ESV-A").ResourceType);
    }

    [Fact]
    public void GetAll_Ncp_HasPsalterResourceType()
    {
        var translations = TranslationCatalog.GetAll();

        var ncp = Assert.Single(translations, t => t.Code == "NCP");
        Assert.Equal(TranslationResourceType.Psalter, ncp.ResourceType);
    }

    [Fact]
    public void GetAll_EsvA_AddsApocryphaOnTopOfBaseCanon()
    {
        var translations = TranslationCatalog.GetAll();

        var esvA = Assert.Single(translations, t => t.Code == "ESV-A");
        Assert.Contains("Ecclesiasticus", esvA.AdditionalBooks);
        Assert.NotEmpty(esvA.AdditionalBooks);
    }

    [Fact]
    public void GetAll_EditionsWithNoCanonExtension_HaveNoAdditionalBooks()
    {
        var translations = TranslationCatalog.GetAll();

        var ncp = Assert.Single(translations, t => t.Code == "NCP");
        Assert.Empty(ncp.AdditionalBooks);
    }
}
