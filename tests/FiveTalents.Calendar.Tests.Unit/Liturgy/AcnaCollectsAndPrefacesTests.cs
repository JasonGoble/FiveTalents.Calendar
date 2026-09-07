using FiveTalents.Calendar.Liturgy;

namespace FiveTalents.Calendar.Tests.Unit.Liturgy;

public sealed class AcnaCollectsAndPrefacesTests
{
    [Theory]
    [InlineData("Advent1")]
    [InlineData("Advent2")]
    [InlineData("Advent3")]
    [InlineData("Advent4")]
    public void TryGetCollect_AdventKeys_ReturnsNonEmptyText(string key)
    {
        var collect = AcnaCollectsAndPrefaces.TryGetCollect(key);

        Assert.NotNull(collect);
        Assert.NotEmpty(collect!.Text);
        Assert.Empty(collect.AlternateTexts);
    }

    [Theory]
    [InlineData("Advent1")]
    [InlineData("Advent2")]
    [InlineData("Advent3")]
    [InlineData("Advent4")]
    public void GetPrefaceNames_AdventKeys_ReturnsAdvent(string key)
    {
        var names = AcnaCollectsAndPrefaces.GetPrefaceNames(key);

        Assert.Equal(["Advent"], names);
    }

    [Fact]
    public void TryGetCollect_EmberDays_HasOneAlternateText()
    {
        var collect = AcnaCollectsAndPrefaces.TryGetCollect("EmberDays");

        Assert.NotNull(collect);
        Assert.NotEmpty(collect!.Text);
        Assert.Single(collect.AlternateTexts);
        Assert.NotEqual(collect.Text, collect.AlternateTexts[0]);
    }

    [Fact]
    public void GetPrefaceNames_EmberDays_ReturnsApostlesAndOrdinations()
    {
        var names = AcnaCollectsAndPrefaces.GetPrefaceNames("EmberDays");

        Assert.Equal(["Apostles and Ordinations"], names);
    }

    // ── National Days — cross-checked against #56's transcribed table ──────────

    [Fact]
    public void TryGetCollect_ThanksgivingDay_HasExpectedText()
    {
        var collect = AcnaCollectsAndPrefaces.TryGetCollect("NationalDay_ThanksgivingDay");

        Assert.NotNull(collect);
        Assert.StartsWith("Most merciful Father, we humbly thank you", collect!.Text);
        Assert.Equal(["Rogation Days or Thanksgiving Day"], AcnaCollectsAndPrefaces.GetPrefaceNames("NationalDay_ThanksgivingDay"));
    }

    [Fact]
    public void TryGetCollect_CanadaDay_HasExpectedTextAndTwoPrefaceOptions()
    {
        var collect = AcnaCollectsAndPrefaces.TryGetCollect("NationalDay_CanadaDay");

        Assert.NotNull(collect);
        Assert.StartsWith("Almighty God, whose wisdom and love are over all", collect!.Text);
        Assert.Equal(["Trinity Sunday", "Canada Day or Independence Day"], AcnaCollectsAndPrefaces.GetPrefaceNames("NationalDay_CanadaDay"));
    }

    [Fact]
    public void TryGetCollect_IndependenceDay_HasExpectedTextAndTwoPrefaceOptions()
    {
        var collect = AcnaCollectsAndPrefaces.TryGetCollect("NationalDay_IndependenceDay");

        Assert.NotNull(collect);
        Assert.StartsWith("Lord God, by your providence our founders won their liberties of old", collect!.Text);
        Assert.Equal(["Trinity Sunday", "Canada Day or Independence Day"], AcnaCollectsAndPrefaces.GetPrefaceNames("NationalDay_IndependenceDay"));
    }

    [Fact]
    public void TryGetCollect_RemembranceDayAndMemorialDay_ShareIdenticalCollectText()
    {
        var remembrance = AcnaCollectsAndPrefaces.TryGetCollect("NationalDay_RemembranceDay");
        var memorial = AcnaCollectsAndPrefaces.TryGetCollect("NationalDay_MemorialDay");

        Assert.NotNull(remembrance);
        Assert.NotNull(memorial);
        Assert.Equal(remembrance!.Text, memorial!.Text);
        Assert.Equal(["Remembrance Day or Memorial Day"], AcnaCollectsAndPrefaces.GetPrefaceNames("NationalDay_RemembranceDay"));
        Assert.Equal(["Remembrance Day or Memorial Day"], AcnaCollectsAndPrefaces.GetPrefaceNames("NationalDay_MemorialDay"));
    }

    // ── Unsourced keys degrade gracefully, not with an exception ────────────────

    [Fact]
    public void TryGetCollect_UnknownKey_ReturnsNull()
    {
        Assert.Null(AcnaCollectsAndPrefaces.TryGetCollect("Proper16"));
    }

    [Fact]
    public void TryGetCollect_NullKey_ReturnsNull()
    {
        Assert.Null(AcnaCollectsAndPrefaces.TryGetCollect(null));
    }

    [Fact]
    public void GetPrefaceNames_UnknownKey_ReturnsEmpty()
    {
        Assert.Empty(AcnaCollectsAndPrefaces.GetPrefaceNames("Proper16"));
    }

    [Fact]
    public void GetPrefaceNames_NullKey_ReturnsEmpty()
    {
        Assert.Empty(AcnaCollectsAndPrefaces.GetPrefaceNames(null));
    }

    // ── Preface catalog round-trip ───────────────────────────────────────────

    [Fact]
    public void TryGetPrefaceText_KnownName_ReturnsNonEmptyText()
    {
        string? text = AcnaCollectsAndPrefaces.TryGetPrefaceText("Advent");

        Assert.NotNull(text);
        Assert.NotEmpty(text);
    }

    [Fact]
    public void TryGetPrefaceText_UnknownName_ReturnsNull()
    {
        Assert.Null(AcnaCollectsAndPrefaces.TryGetPrefaceText("Not A Real Preface"));
    }
}
