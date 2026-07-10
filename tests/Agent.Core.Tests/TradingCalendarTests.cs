using Agent.Core;
using FluentAssertions;

namespace Agent.Core.Tests;

public class TradingCalendarTests
{
    // 2026-07-10 是周五，2026-07-11 周六，2026-07-12 周日，2026-07-13 周一。
    [Fact]
    public void IsTradingDay_Weekday_NotHoliday_ReturnsTrue()
    {
        TradingCalendar.IsTradingDay(new DateOnly(2026, 7, 10), []).Should().BeTrue();
    }

    [Theory]
    [InlineData(2026, 7, 11)] // Sat
    [InlineData(2026, 7, 12)] // Sun
    public void IsTradingDay_Weekend_ReturnsFalse(int y, int m, int d)
    {
        TradingCalendar.IsTradingDay(new DateOnly(y, m, d), []).Should().BeFalse();
    }

    [Fact]
    public void IsTradingDay_ConfiguredHoliday_ReturnsFalse()
    {
        var holidays = new[] { new DateOnly(2026, 10, 1) }; // 国庆（周四）
        TradingCalendar.IsTradingDay(new DateOnly(2026, 10, 1), holidays).Should().BeFalse();
    }

    [Fact]
    public void IsTradingDay_WeekdayNotInHolidayList_ReturnsTrue()
    {
        var holidays = new[] { new DateOnly(2026, 10, 1) };
        TradingCalendar.IsTradingDay(new DateOnly(2026, 7, 13), holidays).Should().BeTrue();
    }
}
