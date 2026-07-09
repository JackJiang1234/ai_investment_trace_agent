using Agent.Core;
using FluentAssertions;

namespace Agent.Core.Tests;

public class AlertEvaluatorTests
{
    private static Quote QuoteWith(decimal changePercent, decimal volumeRatio) => new()
    {
        Code = StockCode.Parse("600519.SH"),
        Name = "测试",
        Price = 100m,
        PreviousClose = 100m,
        Open = 100m,
        High = 100m,
        Low = 100m,
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
    public void Evaluate_PriceSurge_TriggersPriceChangeAlert()
    {
        var alerts = AlertEvaluator.Evaluate(QuoteWith(6.2m, 1m), Options);

        alerts.Should().ContainSingle()
            .Which.Kind.Should().Be(AlertKind.PriceChange);
    }

    [Fact]
    public void Evaluate_PriceDrop_TriggersOnAbsoluteThreshold()
    {
        var alerts = AlertEvaluator.Evaluate(QuoteWith(-5.5m, 1m), Options);

        alerts.Should().ContainSingle().Which.Kind.Should().Be(AlertKind.PriceChange);
    }

    [Fact]
    public void Evaluate_HighVolumeRatio_TriggersVolumeAlert()
    {
        var alerts = AlertEvaluator.Evaluate(QuoteWith(1m, 2.5m), Options);

        alerts.Should().ContainSingle().Which.Kind.Should().Be(AlertKind.VolumeRatio);
    }

    [Fact]
    public void Evaluate_AtThreshold_IsInclusive()
    {
        // |涨跌幅| == 阈值、量比 == 阈值 均视为命中
        var alerts = AlertEvaluator.Evaluate(QuoteWith(5m, 2m), Options);

        alerts.Should().HaveCount(2);
    }

    [Fact]
    public void Evaluate_BelowThresholds_ReturnsEmpty()
    {
        AlertEvaluator.Evaluate(QuoteWith(4.9m, 1.9m), Options).Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_NonPositiveThreshold_DisablesThatRule()
    {
        var disabled = new AlertOptions { PctChangeThreshold = 0m, VolumeRatioThreshold = 0m };

        AlertEvaluator.Evaluate(QuoteWith(20m, 20m), disabled).Should().BeEmpty();
    }
}
