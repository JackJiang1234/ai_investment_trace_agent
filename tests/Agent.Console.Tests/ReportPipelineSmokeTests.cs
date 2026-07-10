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
        var fx = Substitute.For<IExchangeRateSource>();
        fx.GetHkdToCnyAsync(Arg.Any<CancellationToken>()).Returns((decimal?)0.9m);
        var announcements = Substitute.For<IAnnouncementSource>();
        announcements.GetRecentAnnouncementsAsync(Arg.Any<StockCode>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new Announcement[]
            {
                new() { Date = new(2026, 7, 8), Title = "腾讯控股:关于主要股东减持股份的公告", Type = "减持", Url = "https://x" },
            });
        var financials = Substitute.For<IFinancialSource>();
        financials.GetFinancialsAsync(Arg.Any<StockCode>(), Arg.Any<CancellationToken>())
            .Returns(new FinancialInfo
            {
                Snapshot = new FinancialSnapshot
                {
                    ReportPeriod = new(2026, 3, 31),
                    PeriodLabel = "2026年 一季报",
                    NoticeDate = new(2026, 4, 25),
                    Revenue = 54_702_912_385m,
                    NetProfit = 27_242_512_886m,
                    RevenueYoY = 6.34m,
                    NetProfitYoY = 1.47m,
                    Roe = 10.57m,
                },
            });

        var options = new AgentOptions
        {
            Stocks = [new StockConfig { Code = "00700.HK" }],
            Report = new ReportOptions
            {
                OutputDirectory = _dir,
                Formats = [ReportFormat.Html, ReportFormat.Markdown],
            },
        };

        var orchestrator = new ReportOrchestrator(
            quotes, klines, fx, announcements, financials,
            new ScribanReportRenderer(),
            new FileReportDelivery(_dir),
            new NoOpSummarizer(),
            options,
            NullLogger<ReportOrchestrator>.Instance);

        await orchestrator.RunAsync(new DateOnly(2026, 7, 8));

        var html = await File.ReadAllTextAsync(Path.Combine(_dir, "2026-07-08.html"));
        var md = await File.ReadAllTextAsync(Path.Combine(_dir, "2026-07-08.md"));

        html.Should().Contain("腾讯控股");
        html.Should().Contain("风险提示").And.Contain("减持"); // 风险提示区
        html.Should().Contain("近期公告");
        md.Should().Contain("腾讯控股").And.Contain("2026-07-08");
        md.Should().Contain("风险提示").And.Contain("减持");
        md.Should().Contain("财报").And.Contain("2026年 一季报");
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }
}
