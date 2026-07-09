using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;

namespace FiveTalents.Calendar.Tests.Api;

public sealed class CalendarEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    // ── GET /calendar/traditions ──────────────────────────────────────────────

    [Fact]
    public async Task GetTraditions_ReturnsAcnaBcp2019Entry()
    {
        HttpResponseMessage response = await _client.GetAsync("/calendar/traditions");

        response.EnsureSuccessStatusCode();

        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = doc.RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(1, root.GetArrayLength());
        Assert.Equal("AcnaBcp2019", root[0].GetProperty("tradition").GetString());
        Assert.Equal("ACNA Book of Common Prayer 2019", root[0].GetProperty("name").GetString());
    }

    // ── GET /translations ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetTranslations_ReturnsKnownCodes()
    {
        HttpResponseMessage response = await _client.GetAsync("/translations");

        response.EnsureSuccessStatusCode();

        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = doc.RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        JsonElement ncp = root.EnumerateArray().First(e => e.GetProperty("code").GetString() == "NCP");
        Assert.Equal("New Coverdale Psalter", ncp.GetProperty("name").GetString());
        Assert.Equal("Psalter", ncp.GetProperty("resourceType").GetString());

        JsonElement esvA = root.EnumerateArray().First(e => e.GetProperty("code").GetString() == "ESV-A");
        Assert.Equal("Bible", esvA.GetProperty("resourceType").GetString());
        Assert.Contains("Ecclesiasticus", esvA.GetProperty("additionalBooks").EnumerateArray().Select(b => b.GetString()));
    }

    // ── GET /calendar/{tradition}/day/{date} ──────────────────────────────────

    [Fact]
    public async Task GetDay_AdventSunday_ReturnsCorrectSeasonAndYear()
    {
        // First Sunday of Advent 2025 = Nov 30 (Year A)
        HttpResponseMessage response = await _client.GetAsync("/calendar/AcnaBcp2019/day/2025-11-30");

        response.EnsureSuccessStatusCode();

        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = doc.RootElement;

        Assert.Equal("Advent", root.GetProperty("season").GetString());
        Assert.Equal("A", root.GetProperty("week").GetProperty("lectionaryYear").GetString());
    }

    [Fact]
    public async Task GetDay_EnumsSerializeAsStrings()
    {
        // Good Friday — has feast with color, readings with ReadingType
        HttpResponseMessage response = await _client.GetAsync("/calendar/AcnaBcp2019/day/2026-04-03");

        response.EnsureSuccessStatusCode();

        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = doc.RootElement;

        // Season is a string, not an integer
        Assert.Equal(JsonValueKind.String, root.GetProperty("season").ValueKind);
        Assert.Equal("HolyWeek", root.GetProperty("season").GetString());

        // Feast color is a string
        Assert.Equal(JsonValueKind.String, root.GetProperty("feast").GetProperty("color").ValueKind);

        // ReadingType is a string
        JsonElement firstReading = root.GetProperty("readings")[0].GetProperty("readings")[0];
        Assert.Equal(JsonValueKind.String, firstReading.GetProperty("type").ValueKind);
        Assert.Equal("FirstLesson", firstReading.GetProperty("type").GetString());
    }

    [Fact]
    public async Task GetDay_PalmSunday_ReturnsTwoNamedServices()
    {
        // Palm Sunday 2026 = March 29
        HttpResponseMessage response = await _client.GetAsync("/calendar/AcnaBcp2019/day/2026-03-29");

        response.EnsureSuccessStatusCode();

        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement readings = doc.RootElement.GetProperty("readings");

        Assert.Equal(2, readings.GetArrayLength());
        Assert.Equal("Liturgy of the Palms", readings[0].GetProperty("name").GetString());
        Assert.Equal("Liturgy of the Word", readings[1].GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetDay_Psalm_HasNcpTranslationCode()
    {
        HttpResponseMessage response = await _client.GetAsync("/calendar/AcnaBcp2019/day/2025-11-30");

        response.EnsureSuccessStatusCode();

        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement readings = doc.RootElement.GetProperty("readings")[0].GetProperty("readings");
        JsonElement psalm = readings.EnumerateArray().First(r => r.GetProperty("type").GetString() == "Psalm");

        Assert.Equal("NCP", psalm.GetProperty("translationCode").GetString());
    }

    [Fact]
    public async Task GetDay_UnknownTradition_Returns404()
    {
        HttpResponseMessage response = await _client.GetAsync("/calendar/Unknown/day/2026-01-01");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── GET /calendar/{tradition}/range ───────────────────────────────────────

    [Fact]
    public async Task GetRange_ReturnsCorrectDayCount()
    {
        HttpResponseMessage response = await _client.GetAsync(
            "/calendar/AcnaBcp2019/range?from=2026-12-01&to=2026-12-07");

        response.EnsureSuccessStatusCode();

        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(7, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetRange_ToBeforeFrom_Returns400()
    {
        HttpResponseMessage response = await _client.GetAsync(
            "/calendar/AcnaBcp2019/range?from=2026-12-07&to=2026-12-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetRange_ExceedsMaxDays_Returns400()
    {
        HttpResponseMessage response = await _client.GetAsync(
            "/calendar/AcnaBcp2019/range?from=2026-01-01&to=2027-02-15");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetRange_UnknownTradition_Returns404()
    {
        HttpResponseMessage response = await _client.GetAsync(
            "/calendar/Unknown/range?from=2026-01-01&to=2026-01-07");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── GET /calendar/{tradition}/easter/{year} ───────────────────────────────

    [Fact]
    public async Task GetEaster_2026_ReturnsApril5()
    {
        HttpResponseMessage response = await _client.GetAsync("/calendar/AcnaBcp2019/easter/2026");

        response.EnsureSuccessStatusCode();

        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = doc.RootElement;

        Assert.Equal(2026, root.GetProperty("year").GetInt32());
        Assert.Equal("2026-04-05", root.GetProperty("date").GetString());
    }

    [Fact]
    public async Task GetEaster_UnknownTradition_Returns404()
    {
        HttpResponseMessage response = await _client.GetAsync("/calendar/Unknown/easter/2026");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEaster_YearZero_Returns400()
    {
        HttpResponseMessage response = await _client.GetAsync("/calendar/AcnaBcp2019/easter/0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
