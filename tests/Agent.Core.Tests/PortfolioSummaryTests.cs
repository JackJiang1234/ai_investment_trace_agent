using Agent.Core;
using FluentAssertions;

namespace Agent.Core.Tests;

public class PortfolioSummaryTests
{
    private static StockAnalysis Analysis(string code, string name, decimal changePercent) => new()
    {
        Quote = new Quote
        {
            Code = StockCode.Parse(code),
            Name = name,
            Price = 100m,
            PreviousClose = 100m,
            Open = 100m,
            High = 100m,
            Low = 100m,
            ChangeAmount = 0m,
            ChangePercent = changePercent,
            Volume = 0,
            Turnover = 0m,
            TurnoverRate = 0m,
            VolumeRatio = 0m,
        },
    };

    [Fact]
    public void From_CountsAdvancersDeclinersUnchanged()
    {
        var summary = PortfolioSummary.From(
        [
            Analysis("600519.SH", "涨", 3m),
            Analysis("000001.SZ", "跌", -2m),
            Analysis("00700.HK", "平", 0m),
            Analysis("600036.SH", "涨2", 1m),
        ]);

        summary.Advancers.Should().Be(2);
        summary.Decliners.Should().Be(1);
        summary.Unchanged.Should().Be(1);
    }

    [Fact]
    public void From_ComputesAverageChangePercent()
    {
        var summary = PortfolioSummary.From(
        [
            Analysis("600519.SH", "a", 4m),
            Analysis("000001.SZ", "b", -2m),
        ]);

        summary.AverageChangePercent.Should().Be(1m); // (4 + -2) / 2
    }

    [Fact]
    public void From_IdentifiesTopGainerAndLoser()
    {
        var summary = PortfolioSummary.From(
        [
            Analysis("600519.SH", "中", 1m),
            Analysis("000001.SZ", "最强", 8m),
            Analysis("00700.HK", "最弱", -6m),
        ]);

        summary.TopGainer!.Quote.Name.Should().Be("最强");
        summary.TopLoser!.Quote.Name.Should().Be("最弱");
    }

    [Fact]
    public void From_Empty_ReturnsZeroedSummary()
    {
        var summary = PortfolioSummary.From([]);

        summary.Advancers.Should().Be(0);
        summary.Decliners.Should().Be(0);
        summary.Unchanged.Should().Be(0);
        summary.AverageChangePercent.Should().Be(0m);
        summary.TopGainer.Should().BeNull();
        summary.TopLoser.Should().BeNull();
    }
}
