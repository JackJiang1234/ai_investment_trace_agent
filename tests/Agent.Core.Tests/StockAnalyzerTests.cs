using Agent.Core;
using FluentAssertions;

namespace Agent.Core.Tests;

public class StockAnalyzerTests
{
    private static DailyBar Bar(int day, decimal close, decimal high, decimal low) => new()
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

    private static Quote QuoteWith(decimal price, decimal changePercent, decimal volumeRatio) => new()
    {
        Code = StockCode.Parse("600519.SH"),
        Name = "贵州茅台",
        Price = price,
        PreviousClose = price,
        Open = price,
        High = price,
        Low = price,
        ChangeAmount = 0m,
        ChangePercent = changePercent,
        Volume = 1000,
        Turnover = 100000m,
        TurnoverRate = 1m,
        VolumeRatio = volumeRatio,
    };

    private static readonly AlertOptions Options = new()
    {
        PctChangeThreshold = 5m,
        VolumeRatioThreshold = 2m,
    };

    [Fact]
    public void Analyze_ComputesMovingAveragesFromBars()
    {
        // 5 根收盘价 10,20,30,40,50 → MA5 = 30
        DailyBar[] bars =
        [
            Bar(1, 10, 10, 10), Bar(2, 20, 20, 20), Bar(3, 30, 30, 30),
            Bar(4, 40, 40, 40), Bar(5, 50, 50, 50),
        ];

        var analysis = StockAnalyzer.Analyze(QuoteWith(50m, 1m, 1m), bars, Options);

        analysis.Ma5.Should().Be(30m);
        analysis.Ma20.Should().BeNull();   // 数据不足 20 根
        analysis.Quote.Name.Should().Be("贵州茅台");
    }

    [Fact]
    public void Analyze_SurgeQuote_CarriesAlert()
    {
        DailyBar[] bars = [Bar(1, 10, 10, 10)];

        var analysis = StockAnalyzer.Analyze(QuoteWith(10m, 6m, 1m), bars, Options);

        analysis.HasAlert.Should().BeTrue();
        analysis.Alerts.Should().ContainSingle().Which.Kind.Should().Be(AlertKind.PriceChange);
    }

    [Fact]
    public void Analyze_QuietQuote_HasNoAlert()
    {
        DailyBar[] bars = [Bar(1, 10, 10, 10)];

        var analysis = StockAnalyzer.Analyze(QuoteWith(10m, 1m, 1m), bars, Options);

        analysis.HasAlert.Should().BeFalse();
    }

    [Fact]
    public void Analyze_EmptyBars_LeavesIndicatorsNull()
    {
        var analysis = StockAnalyzer.Analyze(QuoteWith(10m, 1m, 1m), [], Options);

        analysis.Ma5.Should().BeNull();
        analysis.Return5.Should().BeNull();
        analysis.IsNewHigh20.Should().BeFalse();
    }
}
