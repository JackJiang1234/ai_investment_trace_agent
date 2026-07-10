using Agent.Core;
using FluentAssertions;

namespace Agent.Core.Tests;

public class PortfolioSummaryTests
{
    private static Quote Quote(string code) => new()
    {
        Code = StockCode.Parse(code),
        Name = code,
        Price = 100m,
        PreviousClose = 100m,
        Open = 100m,
        High = 100m,
        Low = 100m,
        ChangeAmount = 0m,
        ChangePercent = 0m,
        Volume = 0,
        Turnover = 0m,
        TurnoverRate = 0m,
        VolumeRatio = 0m,
    };

    private static StockAnalysis Stock(
        string code, decimal? holdingValueCny, bool canBuy = false, bool shouldSell = false) => new()
    {
        Quote = Quote(code),
        Holding = new HoldingMetrics
        {
            Currency = Currency.CNY,
            HoldingValueCny = holdingValueCny,
            CanBuy = canBuy,
            ShouldSell = shouldSell,
        },
    };

    [Fact]
    public void From_CountsHoldingsBuyAndSellSignals()
    {
        var summary = PortfolioSummary.From(
        [
            Stock("600519.SH", 120000m, canBuy: true),
            Stock("00700.HK", 54000m, shouldSell: true),
            Stock("00388.HK", null, canBuy: true), // 观察池：击球但无持仓
        ]);

        summary.HoldingCount.Should().Be(2);         // 仅两只有持仓
        summary.BuySignalCount.Should().Be(2);       // 两只击球
        summary.SellSignalCount.Should().Be(1);
        summary.TotalHoldingValueCny.Should().Be(174000m);
    }

    [Fact]
    public void From_Empty_ReturnsZeroedSummary()
    {
        var summary = PortfolioSummary.From([]);

        summary.HoldingCount.Should().Be(0);
        summary.BuySignalCount.Should().Be(0);
        summary.SellSignalCount.Should().Be(0);
        summary.TotalHoldingValueCny.Should().Be(0m);
    }
}
