using Agent.Core;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Agent.Core.Tests;

public class ReportOrchestratorTests
{
    private readonly IQuoteSource _quotes = Substitute.For<IQuoteSource>();
    private readonly IKlineSource _klines = Substitute.For<IKlineSource>();
    private readonly IFundFlowSource _fundFlows = Substitute.For<IFundFlowSource>();
    private readonly IAnnouncementSource _announcements = Substitute.For<IAnnouncementSource>();
    private readonly IReportRenderer _renderer = Substitute.For<IReportRenderer>();
    private readonly IReportDelivery _delivery = Substitute.For<IReportDelivery>();
    private readonly ISummarizer _summarizer = Substitute.For<ISummarizer>();

    private static readonly DateOnly Date = new(2026, 7, 8);

    public ReportOrchestratorTests()
    {
        // 安全默认：无公告（避免 null）；个别测试可覆盖。
        _announcements.GetRecentAnnouncementsAsync(Arg.Any<StockCode>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Announcement>());
    }

    private static Quote Quote(string code, decimal price, decimal changePercent = 1m) => new()
    {
        Code = StockCode.Parse(code),
        Name = code,
        Price = price,
        PreviousClose = price,
        Open = price,
        High = price,
        Low = price,
        ChangeAmount = 0m,
        ChangePercent = changePercent,
        Volume = 0,
        Turnover = 0m,
        TurnoverRate = 0m,
        VolumeRatio = 0m,
    };

    private static DailyBar Bar(decimal close, decimal changePercent = 2m) => new()
    {
        Date = Date,
        Open = close,
        Close = close,
        High = close,
        Low = close,
        Volume = 0,
        Amount = 0,
        ChangePercent = changePercent,
        TurnoverRate = 0,
    };

    private ReportOrchestrator Create(AgentOptions options)
    {
        _renderer.Render(Arg.Any<DailyReport>(), Arg.Any<ReportFormat>())
            .Returns(ci => $"[{ci.ArgAt<ReportFormat>(1)}]");
        _summarizer.SummarizeAsync(Arg.Any<DailyReport>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);
        return new ReportOrchestrator(_quotes, _klines, _fundFlows, _announcements,
            _renderer, _delivery, _summarizer, options, NullLogger<ReportOrchestrator>.Instance);
    }

    private static AgentOptions OptionsFor(params string[] codes) => new()
    {
        Stocks = codes.Select(c => new StockConfig { Code = c }).ToList(),
        Report = new ReportOptions { Formats = [ReportFormat.Html, ReportFormat.Markdown] },
    };

    [Fact]
    public async Task RunAsync_BuildsReportForEachConfiguredStock()
    {
        _quotes.GetQuoteAsync(Arg.Any<StockCode>(), Arg.Any<CancellationToken>())
            .Returns(ci => Quote(ci.ArgAt<StockCode>(0).ToString(), 100m));
        _klines.GetDailyBarsAsync(Arg.Any<StockCode>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([Bar(100m)]);

        var report = await Create(OptionsFor("600519.SH", "00700.HK")).RunAsync(Date);

        report.Stocks.Should().HaveCount(2);
        report.Date.Should().Be(Date);
    }

    [Fact]
    public async Task RunAsync_SkipsStockThatThrows_WithoutFailingWholeRun()
    {
        _quotes.GetQuoteAsync(Arg.Is<StockCode>(c => c.Symbol == "600519"), Arg.Any<CancellationToken>())
            .Returns(Quote("600519.SH", 100m));
        _quotes.GetQuoteAsync(Arg.Is<StockCode>(c => c.Symbol == "000001"), Arg.Any<CancellationToken>())
            .Returns<Quote?>(_ => throw new HttpRequestException("boom"));
        _klines.GetDailyBarsAsync(Arg.Any<StockCode>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([Bar(100m)]);

        var report = await Create(OptionsFor("600519.SH", "000001.SZ")).RunAsync(Date);

        report.Stocks.Should().ContainSingle().Which.Quote.Code.Symbol.Should().Be("600519");
    }

    [Fact]
    public async Task RunAsync_NullQuote_IsSkipped()
    {
        _quotes.GetQuoteAsync(Arg.Any<StockCode>(), Arg.Any<CancellationToken>()).Returns((Quote?)null);

        var report = await Create(OptionsFor("600519.SH")).RunAsync(Date);

        report.Stocks.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_ZeroPrice_FallsBackToLatestBarClose()
    {
        // 盘前现价 f43=0 → 用最新K线收盘价回退
        _quotes.GetQuoteAsync(Arg.Any<StockCode>(), Arg.Any<CancellationToken>())
            .Returns(Quote("600519.SH", price: 0m, changePercent: 0m));
        _klines.GetDailyBarsAsync(Arg.Any<StockCode>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([Bar(close: 1199.30m, changePercent: 0.88m)]);

        var report = await Create(OptionsFor("600519.SH")).RunAsync(Date);

        var s = report.Stocks.Should().ContainSingle().Subject;
        s.Quote.Price.Should().Be(1199.30m);
        s.Quote.ChangePercent.Should().Be(0.88m);
    }

    [Fact]
    public async Task RunAsync_RendersAndDeliversEachConfiguredFormat()
    {
        _quotes.GetQuoteAsync(Arg.Any<StockCode>(), Arg.Any<CancellationToken>())
            .Returns(Quote("600519.SH", 100m));
        _klines.GetDailyBarsAsync(Arg.Any<StockCode>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([Bar(100m)]);

        await Create(OptionsFor("600519.SH")).RunAsync(Date);

        await _delivery.Received(1).DeliverAsync(
            Arg.Is<RenderedReport>(r =>
                r.Contents.ContainsKey(ReportFormat.Html) &&
                r.Contents.ContainsKey(ReportFormat.Markdown)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_AttachesFundFlowToAnalysis()
    {
        _quotes.GetQuoteAsync(Arg.Any<StockCode>(), Arg.Any<CancellationToken>())
            .Returns(Quote("600519.SH", 100m));
        _klines.GetDailyBarsAsync(Arg.Any<StockCode>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([Bar(100m)]);
        var flow = new FundFlow
        {
            Date = Date,
            MainNetInflow = -388600352m,
            SuperLargeNetInflow = -393122096m,
            LargeNetInflow = 4521744m,
            MediumNetInflow = 388776592m,
            SmallNetInflow = -176247m,
        };
        _fundFlows.GetLatestFundFlowAsync(Arg.Any<StockCode>(), Arg.Any<CancellationToken>())
            .Returns(flow);

        var report = await Create(OptionsFor("600519.SH")).RunAsync(Date);

        report.Stocks.Should().ContainSingle()
            .Which.FundFlow!.MainNetInflow.Should().Be(-388600352m);
    }

    [Fact]
    public async Task RunAsync_FundFlowFailure_DegradesToNull_WithoutDroppingStock()
    {
        _quotes.GetQuoteAsync(Arg.Any<StockCode>(), Arg.Any<CancellationToken>())
            .Returns(Quote("600519.SH", 100m));
        _klines.GetDailyBarsAsync(Arg.Any<StockCode>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([Bar(100m)]);
        _fundFlows.GetLatestFundFlowAsync(Arg.Any<StockCode>(), Arg.Any<CancellationToken>())
            .Returns<FundFlow?>(_ => throw new HttpRequestException("fund flow down"));

        var report = await Create(OptionsFor("600519.SH")).RunAsync(Date);

        var stock = report.Stocks.Should().ContainSingle().Subject;
        stock.FundFlow.Should().BeNull();
        stock.Quote.Code.Symbol.Should().Be("600519");
    }

    [Fact]
    public async Task RunAsync_RiskKeywordInAnnouncement_ProducesRiskHit()
    {
        _quotes.GetQuoteAsync(Arg.Any<StockCode>(), Arg.Any<CancellationToken>())
            .Returns(Quote("600519.SH", 100m));
        _klines.GetDailyBarsAsync(Arg.Any<StockCode>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([Bar(100m)]);
        _announcements.GetRecentAnnouncementsAsync(Arg.Any<StockCode>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new Announcement[]
            {
                new() { Date = Date, Title = "关于控股股东股份质押的公告", Type = "股权质押", Url = "https://x" },
                new() { Date = Date, Title = "关于召开股东大会的通知", Type = "日常经营", Url = "https://y" },
            });

        var report = await Create(OptionsFor("600519.SH")).RunAsync(Date);

        report.RiskHits.Should().ContainSingle();
        report.RiskHits[0].MatchedKeyword.Should().Be("质押");
        report.RiskHits[0].StockName.Should().Be("600519.SH");
        // 重要类型过滤：日常经营公告未命中 IncludeTypes，不进个股公告列表
        report.Stocks[0].Announcements.Should().ContainSingle()
            .Which.Title.Should().Contain("质押");
    }
}
