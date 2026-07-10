using Microsoft.Extensions.Logging;

namespace Agent.Core;

/// <summary>
/// 报告编排：解析汇率 → 逐只股票拉取行情/K线/公告/财报并计算持仓与买卖点 →
/// 汇总（分组、组合概览、持股比例）→ 渲染 → 投递。
/// 单只股票失败不影响整体（记录并跳过），符合数据源易变的容错要求。
/// </summary>
public sealed class ReportOrchestrator
{
    private readonly IQuoteSource _quoteSource;
    private readonly IKlineSource _klineSource;
    private readonly IExchangeRateSource _exchangeRateSource;
    private readonly IAnnouncementSource _announcementSource;
    private readonly IFinancialSource _financialSource;
    private readonly IReportRenderer _renderer;
    private readonly IReportDelivery _delivery;
    private readonly ISummarizer _summarizer;
    private readonly AgentOptions _options;
    private readonly ILogger<ReportOrchestrator> _logger;

    public ReportOrchestrator(
        IQuoteSource quoteSource,
        IKlineSource klineSource,
        IExchangeRateSource exchangeRateSource,
        IAnnouncementSource announcementSource,
        IFinancialSource financialSource,
        IReportRenderer renderer,
        IReportDelivery delivery,
        ISummarizer summarizer,
        AgentOptions options,
        ILogger<ReportOrchestrator> logger)
    {
        _quoteSource = quoteSource;
        _klineSource = klineSource;
        _exchangeRateSource = exchangeRateSource;
        _announcementSource = announcementSource;
        _financialSource = financialSource;
        _renderer = renderer;
        _delivery = delivery;
        _summarizer = summarizer;
        _options = options;
        _logger = logger;
    }

    public async Task<DailyReport> RunAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var converter = await ResolveConverterAsync(cancellationToken);

        var analyses = new List<StockAnalysis>(_options.Stocks.Count);
        foreach (var stock in _options.Stocks)
        {
            var analysis = await AnalyzeStockAsync(stock, converter, date, cancellationToken);
            if (analysis is not null)
            {
                analyses.Add(analysis);
            }
        }

        analyses = ApplyHoldingRatios(analyses);

        var report = new DailyReport
        {
            Date = date,
            Summary = PortfolioSummary.From(analyses),
            Stocks = analyses,
            RiskHits = CollectRiskHits(analyses),
            HkdToCny = converter.HkdToCny,
            IsLiveRate = converter.IsLiveRate,
        };

        var summary = await _summarizer.SummarizeAsync(report, cancellationToken);
        if (!string.IsNullOrWhiteSpace(summary))
        {
            report = report with { AiSummary = summary };
        }

        var contents = new Dictionary<ReportFormat, string>();
        foreach (var format in _options.Report.Formats)
        {
            contents[format] = _renderer.Render(report, format);
        }

        await _delivery.DeliverAsync(
            new RenderedReport { Date = date, Contents = contents }, cancellationToken);

        _logger.LogInformation("报告生成完成：{Count} 只股票，击球 {Buy} 只。",
            analyses.Count, report.Buyable.Count);

        return report;
    }

    /// <summary>解析 HKD→CNY 汇率：优先实时源，失败回退配置静态汇率。</summary>
    private async Task<CurrencyConverter> ResolveConverterAsync(CancellationToken ct)
    {
        try
        {
            if (await _exchangeRateSource.GetHkdToCnyAsync(ct) is { } live && live > 0)
            {
                return new CurrencyConverter(live, isLiveRate: true);
            }

            _logger.LogWarning("汇率源无数据，回退静态汇率。");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "汇率获取失败，回退静态汇率。");
        }

        var fallback = _options.Currency.StaticRates.TryGetValue("HKD", out var s) && s > 0 ? s : 0.87m;
        return new CurrencyConverter(fallback, isLiveRate: false);
    }

    private async Task<StockAnalysis?> AnalyzeStockAsync(
        StockConfig stock, CurrencyConverter converter, DateOnly date, CancellationToken ct)
    {
        StockCode code;
        try
        {
            code = StockCode.Parse(stock.Code);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "跳过非法股票代码：{Code}", stock.Code);
            return null;
        }

        try
        {
            var quote = await _quoteSource.GetQuoteAsync(code, ct);
            if (quote is null)
            {
                _logger.LogWarning("无行情数据，跳过：{Code}", code);
                return null;
            }

            var bars = await _klineSource.GetDailyBarsAsync(code, _options.Report.KlineLookbackDays, ct);
            quote = ApplyClosePriceFallback(quote, bars);

            var yearStartClose = await TryGetYearStartCloseAsync(code, date.Year, ct);

            return new StockAnalysis
            {
                Quote = quote,
                Holding = HoldingCalculator.Compute(quote, stock, converter, yearStartClose),
                Announcements = await TryGetAnnouncementsAsync(code, date, ct),
                Financials = await TryGetFinancialsAsync(code, ct),
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "拉取失败，跳过：{Code}", code);
            return null;
        }
    }

    /// <summary>取当年首个交易日收盘价供 YTD；失败降级为 null（该股不显示年内涨幅）。</summary>
    private async Task<decimal?> TryGetYearStartCloseAsync(StockCode code, int year, CancellationToken ct)
    {
        try
        {
            return await _klineSource.GetYearStartCloseAsync(code, year, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "年初收盘获取失败，年内涨幅降级为空：{Code}", code);
            return null;
        }
    }

    /// <summary>
    /// 拉取公告：按市场重要类型过滤，再按内容时间窗（近 LookbackDays 天）过滤。失败降级为空。
    /// </summary>
    private async Task<IReadOnlyList<Announcement>> TryGetAnnouncementsAsync(
        StockCode code, DateOnly date, CancellationToken ct)
    {
        try
        {
            var raw = await _announcementSource.GetRecentAnnouncementsAsync(
                code, _options.Announcements.Lookback, ct);

            // 过滤按市场区分：A股用中文重要类型包含匹配；港股（英文标题）改为排除例行件。
            var important = code.Market == Market.HK
                ? AnnouncementFilter.ExcludeRoutine(raw, _options.Announcements.HkExcludeTypes)
                : AnnouncementFilter.FilterImportant(raw, _options.Announcements);

            // 周跟踪内容时间窗：仅保留近 N 天（本周）的公告。
            var cutoff = date.AddDays(-(_options.Announcements.LookbackDays - 1));
            return important.Where(a => a.Date >= cutoff).ToArray();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "公告获取失败，降级为空：{Code}", code);
            return [];
        }
    }

    /// <summary>拉取财报；补充信息，失败降级为 null。</summary>
    private async Task<FinancialInfo?> TryGetFinancialsAsync(StockCode code, CancellationToken ct)
    {
        try
        {
            return await _financialSource.GetFinancialsAsync(code, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "财报获取失败，降级为空：{Code}", code);
            return null;
        }
    }

    /// <summary>
    /// 计算组合内权重（持股比例）：各股持仓市值 ÷ 全部持仓总市值（均人民币）。
    /// 分母仅含有持仓的股票；无持仓总市值时不计比例。
    /// </summary>
    private static List<StockAnalysis> ApplyHoldingRatios(List<StockAnalysis> analyses)
    {
        var total = analyses.Sum(a => a.Holding?.HoldingValueCny ?? 0m);
        if (total <= 0m)
        {
            return analyses;
        }

        return analyses
            .Select(a => a.Holding?.HoldingValueCny is { } value
                ? a with { Holding = a.Holding with { HoldingRatioPercent = value / total * 100m } }
                : a)
            .ToList();
    }

    /// <summary>扫描各股公告标题，汇总命中风险关键词的条目（跨个股）。</summary>
    private IReadOnlyList<RiskHit> CollectRiskHits(IReadOnlyList<StockAnalysis> analyses)
    {
        var hits = new List<RiskHit>();
        foreach (var analysis in analyses)
        {
            foreach (var ann in analysis.Announcements)
            {
                var keyword = RiskKeywordMatcher.FindMatch(ann.Title, _options.RiskKeywords);
                if (keyword is not null)
                {
                    hits.Add(new RiskHit
                    {
                        Code = analysis.Quote.Code,
                        StockName = analysis.Quote.Name,
                        Announcement = ann,
                        MatchedKeyword = keyword,
                    });
                }
            }
        }

        return hits;
    }

    /// <summary>
    /// 非交易时段东财现价 f43 会返回 0；此时用最新一根K线的收盘价与涨跌幅回退，
    /// 保证报告在盘前运行也有意义。收盘后运行时现价已是收盘价，不受影响。
    /// </summary>
    private static Quote ApplyClosePriceFallback(Quote quote, IReadOnlyList<DailyBar> bars)
    {
        if (quote.Price != 0 || bars.Count == 0)
        {
            return quote;
        }

        var last = bars[^1];
        return quote with
        {
            Price = last.Close,
            ChangePercent = quote.ChangePercent == 0 ? last.ChangePercent : quote.ChangePercent,
        };
    }
}
