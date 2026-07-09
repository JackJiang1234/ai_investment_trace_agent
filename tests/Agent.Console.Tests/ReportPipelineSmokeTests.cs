using Agent.Core;
using Agent.Delivery;
using Agent.Reporting;
using Agent.Summarizer;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Agent.Console.Tests;

/// <summary>
/// 端到端管线冒烟：真实 Scriban 渲染 + 真实文件投递 + Mock 数据源。
/// 验证「数据 → 分析 → 渲染 → 落盘」整链在离线下可产出正确文件。
/// </summary>
public class ReportPipelineSmokeTests : IDisposable
{
    private readonly string _dir = Path.Combine(
        Path.GetTempPath(), "agent-pipeline-smoke", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task RunAsync_ProducesHtmlAndMarkdownFilesWithData()
    {
        var quotes = Substitute.For<IQuoteSource>();
        var klines = Substitute.For<IKlineSource>();
        quotes.GetQuoteAsync(Arg.Any<StockCode>(), Arg.Any<CancellationToken>())
            .Returns(new Quote
            {
                Code = StockCode.Parse("00700.HK"),
                Name = "腾讯控股",
                Price = 476.40m,
                PreviousClose = 461.20m,
                Open = 461.20m,
                High = 482.80m,
                Low = 460.60m,
                ChangeAmount = 15.20m,
                ChangePercent = 6.30m,
                Volume = 49_390_003,
                Turnover = 23_486_681_600m,
                TurnoverRate = 0.54m,
                VolumeRatio = 2.5m, // 触发量比异动
            });
        klines.GetDailyBarsAsync(Arg.Any<StockCode>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new DailyBar[]
            {
                new() { Date = new(2026, 7, 7), Open = 459m, Close = 461.2m, High = 479.8m, Low = 457m, Volume = 1, Amount = 1m, ChangePercent = 2.04m, TurnoverRate = 0.6m },
                new() { Date = new(2026, 7, 8), Open = 461.2m, Close = 476.4m, High = 482.8m, Low = 460.6m, Volume = 1, Amount = 1m, ChangePercent = 6.30m, TurnoverRate = 0.54m },
            });
        var fundFlows = Substitute.For<IFundFlowSource>();
        fundFlows.GetLatestFundFlowAsync(Arg.Any<StockCode>(), Arg.Any<CancellationToken>())
            .Returns(new FundFlow
            {
                Date = new(2026, 7, 8),
                MainNetInflow = 980_042_000m,
                SuperLargeNetInflow = 997_388_832m,
                LargeNetInflow = -17_346_832m,
                MediumNetInflow = 1_708_122_368m,
                SmallNetInflow = 514_965_024m,
            });

        var options = new AgentOptions
        {
            Stocks = [new StockConfig { Code = "00700.HK" }],
            Alerts = new AlertOptions { PctChangeThreshold = 5m, VolumeRatioThreshold = 2m },
            Report = new ReportOptions
            {
                OutputDirectory = _dir,
                Formats = [ReportFormat.Html, ReportFormat.Markdown],
            },
        };

        var orchestrator = new ReportOrchestrator(
            quotes, klines, fundFlows,
            new ScribanReportRenderer(),
            new FileReportDelivery(_dir),
            new NoOpSummarizer(),
            options,
            NullLogger<ReportOrchestrator>.Instance);

        await orchestrator.RunAsync(new DateOnly(2026, 7, 8));

        var html = await File.ReadAllTextAsync(Path.Combine(_dir, "2026-07-08.html"));
        var md = await File.ReadAllTextAsync(Path.Combine(_dir, "2026-07-08.md"));

        html.Should().Contain("腾讯控股").And.Contain("6.3");
        html.Should().Contain("量比"); // 异动提醒区
        html.Should().Contain("9.8"); // 主力净流入 9.80 亿
        md.Should().Contain("腾讯控股").And.Contain("2026-07-08");
        md.Should().Contain("主力净流入");
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }
}
