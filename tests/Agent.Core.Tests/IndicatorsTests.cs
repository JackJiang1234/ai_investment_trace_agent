using Agent.Core;
using FluentAssertions;

namespace Agent.Core.Tests;

public class IndicatorsTests
{
    // 构造仅关心 High/Low/Close 的日K线；其余字段填占位值。
    private static DailyBar Bar(int day, decimal high, decimal low, decimal close) => new()
    {
        Date = new DateOnly(2026, 1, day),
        Open = close,
        Close = close,
        High = high,
        Low = low,
        Volume = 0,
        Amount = 0,
        ChangePercent = 0,
        TurnoverRate = 0,
    };

    public class MovingAverage
    {
        [Fact]
        public void AveragesLastPeriodCloses()
        {
            var closes = new decimal[] { 10, 20, 30 };

            Indicators.MovingAverage(closes, 3).Should().Be(20m);
            Indicators.MovingAverage(closes, 2).Should().Be(25m); // (20+30)/2
        }

        [Fact]
        public void ReturnsNull_WhenInsufficientData()
        {
            Indicators.MovingAverage(new decimal[] { 10, 20 }, 3).Should().BeNull();
            Indicators.MovingAverage([], 1).Should().BeNull();
        }

        [Fact]
        public void Throws_WhenPeriodNotPositive()
        {
            var act = () => Indicators.MovingAverage(new decimal[] { 1 }, 0);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }

    public class PeriodReturnPercent
    {
        [Fact]
        public void ComputesReturnOverPeriod()
        {
            // 近 1 日
            Indicators.PeriodReturnPercent(new decimal[] { 100, 110 }, 1).Should().Be(10m);
            // 近 2 日：最新 vs 2 个交易日前
            Indicators.PeriodReturnPercent(new decimal[] { 100, 105, 110 }, 2).Should().Be(10m);
        }

        [Fact]
        public void ReturnsNull_WhenInsufficientData()
        {
            // 近 N 日需要 N+1 个收盘价
            Indicators.PeriodReturnPercent(new decimal[] { 100 }, 1).Should().BeNull();
        }

        [Fact]
        public void ReturnsNull_WhenBasePriceIsZero()
        {
            Indicators.PeriodReturnPercent(new decimal[] { 0, 110 }, 1).Should().BeNull();
        }
    }

    public class PeriodHighLow
    {
        private static readonly DailyBar[] Bars =
        [
            Bar(1, high: 12, low: 8, close: 10),
            Bar(2, high: 14, low: 9, close: 13),
            Bar(3, high: 15, low: 11, close: 14), // 最新
        ];

        [Fact]
        public void IsPeriodHigh_True_WhenLatestHighIsMaxInWindow()
        {
            Indicators.IsPeriodHigh(Bars, 3).Should().BeTrue();
        }

        [Fact]
        public void IsPeriodHigh_False_WhenLatestNotMax()
        {
            DailyBar[] bars = [Bar(1, 20, 5, 18), Bar(2, 15, 6, 14)];
            Indicators.IsPeriodHigh(bars, 2).Should().BeFalse();
        }

        [Fact]
        public void IsPeriodLow_True_WhenLatestLowIsMinInWindow()
        {
            DailyBar[] bars = [Bar(1, 20, 10, 18), Bar(2, 15, 6, 8)];
            Indicators.IsPeriodLow(bars, 2).Should().BeTrue();
        }

        [Fact]
        public void ReturnsFalse_WhenFewerBarsThanPeriod()
        {
            Indicators.IsPeriodHigh(Bars, 20).Should().BeFalse();
            Indicators.IsPeriodLow(Bars, 20).Should().BeFalse();
        }
    }
}
