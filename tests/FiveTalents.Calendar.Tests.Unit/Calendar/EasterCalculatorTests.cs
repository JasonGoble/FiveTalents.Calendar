using FiveTalents.Calendar.Calendar;

namespace FiveTalents.Calendar.Tests.Unit.Calendar;

public sealed class EasterCalculatorTests
{
    // Known Easter dates sourced from the US Naval Observatory and BCP 2019 calendar.
    [Theory]
    [InlineData(2020, 4, 12)]
    [InlineData(2021, 4, 4)]
    [InlineData(2022, 4, 17)]
    [InlineData(2023, 4, 9)]
    [InlineData(2024, 3, 31)]
    [InlineData(2025, 4, 20)]
    [InlineData(2026, 4, 5)]
    [InlineData(2027, 3, 28)]
    [InlineData(2028, 4, 16)]
    [InlineData(2029, 4, 1)]
    public void GetEaster_ReturnsCorrectDate(int year, int month, int day)
    {
        DateOnly expected = new DateOnly(year, month, day);
        Assert.Equal(expected, EasterCalculator.GetEaster(year));
    }
}
