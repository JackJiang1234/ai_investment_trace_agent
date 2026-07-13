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
        string code, decimal? holdingValueCny, bool canBuy = false, bool shouldSell = false,
        decimal? yearStartValueCny = null, decimal? costValueCny = null, decimal? costProfitCny = null) => new()
    {
        Quote = Quote(code),
        Holding = new HoldingMetrics
        {
            Currency = Currency.CNY,
            HoldingValueCny = holdingValueCny,
            YearStartValueCny = yearStartValueCny,
            CostValueCny = costValueCny,
            CostProfitCny = costProfitCny,
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
    public void From_ComputesYtdProfitAndReturn()
    {
        // A: 现值 120000 / 年初 100000 → +20000；B: 现值 54000 / 年初 60000 → -6000
        // 合计收益 14000，年初总市值 160000 → 8.75%
        var summary = PortfolioSummary.From(
        [
            Stock("600519.SH", holdingValueCny: 120000m, yearStartValueCny: 100000m),
            Stock("00700.HK", holdingValueCny: 54000m, yearStartValueCny: 60000m),
            Stock("00388.HK", holdingValueCny: null),   // 观察池：不计入
        ]);

        summary.YtdProfitCny.Should().Be(14000m);
        summary.YtdReturnPercent.Should().Be(8.75m);
    }

    [Fact]
    public void From_ComputesCostProfitAndReturn()
    {
        // A: 成本 100000 盈亏 +20000; B: 成本 50000 盈亏 -5000
        // 合计盈亏 15000, 总成本 150000 → 10%
        var summary = PortfolioSummary.From(
        [
            Stock("600519.SH", holdingValueCny: 120000m, costValueCny: 100000m, costProfitCny: 20000m),
            Stock("00700.HK", holdingValueCny: 54000m, costValueCny: 50000m, costProfitCny: -5000m),
            Stock("00388.HK", holdingValueCny: null),   // 观察池：不计入
        ]);

        summary.CostProfitCny.Should().Be(15000m);
        summary.CostReturnPercent.Should().Be(10m);
    }

    [Fact]
    public void From_NoCostData_CostReturnIsNull()
    {
        var summary = PortfolioSummary.From([Stock("600519.SH", holdingValueCny: 120000m)]);

        summary.CostProfitCny.Should().Be(0m);
        summary.CostReturnPercent.Should().BeNull();
    }

    [Fact]
    public void From_NoYearStartData_YtdReturnIsNull()
    {
        var summary = PortfolioSummary.From([Stock("600519.SH", holdingValueCny: 120000m)]);

        summary.YtdProfitCny.Should().Be(0m);
        summary.YtdReturnPercent.Should().BeNull();
    }

    [Fact]
    public void From_Empty_ReturnsZeroedSummary()
    {
        var summary = PortfolioSummary.From([]);

        summary.HoldingCount.Should().Be(0);
        summary.BuySignalCount.Should().Be(0);
        summary.SellSignalCount.Should().Be(0);
        summary.TotalHoldingValueCny.Should().Be(0m);
        summary.YtdProfitCny.Should().Be(0m);
        summary.YtdReturnPercent.Should().BeNull();
        summary.CostProfitCny.Should().Be(0m);
        summary.CostReturnPercent.Should().BeNull();
    }
}
