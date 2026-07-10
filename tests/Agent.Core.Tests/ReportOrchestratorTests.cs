using Agent.Core;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Agent.Core.Tests;

public class ReportOrchestratorTests
{
    private readonly IQuoteSource _quotes = Substitute.For<IQuoteSource>();
    private readonly IKlineSource _klines = Substitute.For<IKlineSource>();
    private readonly IExchangeRateSource _fx = Substitute.For<IExchangeRateSource>();
    private readonly IAnnouncementSource _announcements = Substitute.For<IAnnouncementSource>();
    private readonly IFinancialSource _financials = Substitute.For<IFinancialSource>();
    private readonly IReportRenderer _renderer = Substitute.For<IReportRenderer>();
    private readonly IReportDelivery _delivery = Substitute.For<IReportDelivery>();
    private readonly ISummarizer _summarizer = Substitute.For<ISummarizer>();

    private static readonly DateOnly Date = new(2026, 7, 8);

    public ReportOrchestratorTests()
    {
        // 安全默认：无公告（避免 null）；个别测试可覆盖。
        _announcements.GetRecentAnnouncementsAsync(Arg.Any<StockCode>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Announcement>());
        // 默认实时汇率 0.9。
        _fx.GetHkdToCnyAsync(Arg.Any<CancellationToken>()).Returns((decimal?)0.9m);
    }

    private static Quote Quote(string code, decimal price, decimal changePercent = 1m, decimal totalMarketCap = 0m) => new()
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
        TotalMarketCap = totalMarketCap,
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
        return new ReportOrchestrator(_quotes, _klines, _fx, _announcements, _financials,
            _renderer, _delivery, _summarizer, options, NullLogger<ReportOrchestrator>.Instance);
    }

    private static AgentOptions OptionsFor(params string[] codes) => new()
    {
        Stocks = codes.Select(c => new StockConfig { Code = c }).ToList(),
        Report = new ReportOptions { Formats = [ReportFormat.Html, ReportFormat.Markdown] },
    };

    private static AgentOptions OptionsFor(params StockConfig[] stocks) => new()
    {
        Stocks = stocks.ToList(),
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

    [Fact]
    public async Task RunAsync_HongKong_ExcludesRoutineAnnouncements()
    {
        _quotes.GetQuoteAsync(Arg.Any<StockCode>(), Arg.Any<CancellationToken>())
            .Returns(Quote("00700.HK", 100m));
        _klines.GetDailyBarsAsync(Arg.Any<StockCode>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([Bar(100m)]);
        _announcements.GetRecentAnnouncementsAsync(Arg.Any<StockCode>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new Announcement[]
            {
                new() { Date = Date, Title = "Next Day Disclosure Return", Type = "Next Day Disclosure Returns - [Share Buyback]", Url = "https://h/1" },
                new() { Date = Date, Title = "Discloseable Transaction", Type = "Announcements and Notices", Url = "https://h/2" },
            });

        var report = await Create(OptionsFor("00700.HK")).RunAsync(Date);

        // 港股走排除例行件：Next Day Disclosure 被剔除，实质公告保留
        report.Stocks[0].Announcements.Should().ContainSingle()
            .Which.Title.Should().Be("Discloseable Transaction");
    }

    [Fact]
    public async Task RunAsync_AttachesFinancials()
    {
        _quotes.GetQuoteAsync(Arg.Any<StockCode>(), Arg.Any<CancellationToken>())
            .Returns(Quote("600519.SH", 100m));
        _klines.GetDailyBarsAsync(Arg.Any<StockCode>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([Bar(100m)]);
        _financials.GetFinancialsAsync(Arg.Any<StockCode>(), Arg.Any<CancellationToken>())
            .Returns(new FinancialInfo
            {
                Snapshot = new FinancialSnapshot
                {
                    ReportPeriod = new DateOnly(2026, 3, 31),
                    PeriodLabel = "2026年 一季报",
                    NoticeDate = new DateOnly(2026, 4, 25),
                    Revenue = 54_702_912_385.23m,
                    NetProfit = 27_242_512_886.45m,
                    RevenueYoY = 6.34m,
                },
            });

        var report = await Create(OptionsFor("600519.SH")).RunAsync(Date);

        var fin = report.Stocks[0].Financials;
        fin.Should().NotBeNull();
        fin!.Snapshot!.PeriodLabel.Should().Be("2026年 一季报");
    }

    [Fact]
    public async Task RunAsync_FinancialsFailure_DegradesToNull()
    {
        _quotes.GetQuoteAsync(Arg.Any<StockCode>(), Arg.Any<CancellationToken>())
            .Returns(Quote("600519.SH", 100m));
        _klines.GetDailyBarsAsync(Arg.Any<StockCode>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([Bar(100m)]);
        _financials.GetFinancialsAsync(Arg.Any<StockCode>(), Arg.Any<CancellationToken>())
            .Returns<FinancialInfo?>(_ => throw new HttpRequestException("fin down"));

        var report = await Create(OptionsFor("600519.SH")).RunAsync(Date);

        report.Stocks.Should().ContainSingle().Which.Financials.Should().BeNull();
    }

    [Fact]
    public async Task RunAsync_ComputesHoldingAndBuySignal()
    {
        // 总市值 1.5万亿元 = 15000亿 < 理想买点 18000 → 击球
        _quotes.GetQuoteAsync(Arg.Any<StockCode>(), Arg.Any<CancellationToken>())
            .Returns(Quote("600519.SH", 1200m, totalMarketCap: 1_500_000_000_000m));
        _klines.GetDailyBarsAsync(Arg.Any<StockCode>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([Bar(1200m)]);

        var options = OptionsFor(new StockConfig
        {
            Code = "600519.SH", Group = "核心持仓", Shares = 100, CostPrice = 1450m,
            IdealBuyMarketCapYi = 18000m, SellMarketCapYi = 28000m,
        });

        var report = await Create(options).RunAsync(Date);

        var h = report.Stocks.Should().ContainSingle().Subject.Holding!;
        h.TotalMarketCapCnyYi.Should().Be(15000m);
        h.CanBuy.Should().BeTrue();
        report.Buyable.Should().ContainSingle();
        report.Summary.BuySignalCount.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_GroupsStocks_CoreFirstThenWatchlist()
    {
        _quotes.GetQuoteAsync(Arg.Any<StockCode>(), Arg.Any<CancellationToken>())
            .Returns(ci => Quote(ci.ArgAt<StockCode>(0).ToString(), 100m, totalMarketCap: 1_000_000_000m));
        _klines.GetDailyBarsAsync(Arg.Any<StockCode>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([Bar(100m)]);

        var options = OptionsFor(
            new StockConfig { Code = "00700.HK", Group = "观察池" },
            new StockConfig { Code = "600519.SH", Group = "核心持仓" });

        var report = await Create(options).RunAsync(Date);

        report.Groups.Should().HaveCount(2);
        report.Groups[0].Name.Should().Be("核心持仓"); // 核心持仓在前，尽管配置顺序在后
        report.Groups[1].Name.Should().Be("观察池");
    }

    [Fact]
    public async Task RunAsync_HoldingRatio_SumsTo100AcrossHoldings()
    {
        _quotes.GetQuoteAsync(Arg.Is<StockCode>(c => c.Symbol == "600519"), Arg.Any<CancellationToken>())
            .Returns(Quote("600519.SH", 100m, totalMarketCap: 1_000_000_000m));
        _quotes.GetQuoteAsync(Arg.Is<StockCode>(c => c.Symbol == "000001"), Arg.Any<CancellationToken>())
            .Returns(Quote("000001.SZ", 100m, totalMarketCap: 1_000_000_000m));
        _klines.GetDailyBarsAsync(Arg.Any<StockCode>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([Bar(100m)]);

        // 各 100×份数：600519 300 股→30000；000001 100 股→10000 → 75% / 25%
        var options = OptionsFor(
            new StockConfig { Code = "600519.SH", Group = "核心持仓", Shares = 300 },
            new StockConfig { Code = "000001.SZ", Group = "核心持仓", Shares = 100 });

        var report = await Create(options).RunAsync(Date);

        var ratios = report.Stocks.Select(s => s.Holding!.HoldingRatioPercent).ToArray();
        ratios[0].Should().Be(75m);
        ratios[1].Should().Be(25m);
    }

    [Fact]
    public async Task RunAsync_FxFailure_FallsBackToStaticRate()
    {
        _fx.GetHkdToCnyAsync(Arg.Any<CancellationToken>())
            .Returns<decimal?>(_ => throw new HttpRequestException("fx down"));
        _quotes.GetQuoteAsync(Arg.Any<StockCode>(), Arg.Any<CancellationToken>())
            .Returns(Quote("600519.SH", 100m));
        _klines.GetDailyBarsAsync(Arg.Any<StockCode>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([Bar(100m)]);

        var options = OptionsFor("600519.SH");
        options.Currency.StaticRates["HKD"] = 0.85m;

        var report = await Create(options).RunAsync(Date);

        report.IsLiveRate.Should().BeFalse();
        report.HkdToCny.Should().Be(0.85m);
    }

    [Fact]
    public async Task RunAsync_FiltersAnnouncementsOutsideLookbackWindow()
    {
        _quotes.GetQuoteAsync(Arg.Any<StockCode>(), Arg.Any<CancellationToken>())
            .Returns(Quote("600519.SH", 100m));
        _klines.GetDailyBarsAsync(Arg.Any<StockCode>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([Bar(100m)]);
        _announcements.GetRecentAnnouncementsAsync(Arg.Any<StockCode>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new Announcement[]
            {
                new() { Date = Date, Title = "本周业绩快报", Type = "快报", Url = "https://a" },
                new() { Date = Date.AddDays(-30), Title = "上月业绩快报", Type = "快报", Url = "https://b" },
            });

        var report = await Create(OptionsFor("600519.SH")).RunAsync(Date);

        report.Stocks[0].Announcements.Should().ContainSingle()
            .Which.Title.Should().Be("本周业绩快报");
    }
}
