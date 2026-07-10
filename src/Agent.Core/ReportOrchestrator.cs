using Microsoft.Extensions.Logging;

namespace Agent.Core;

/// <summary>
/// 报告编排：逐只股票拉取行情与历史K线 → 计算分析 → 汇总 → 渲染 → 投递。
/// 单只股票失败不影响整体（记录并跳过），符合数据源易变的容错要求。
/// </summary>
public sealed class ReportOrchestrator
{
    private readonly IQuoteSource _quoteSource;
    private readonly IKlineSource _klineSource;
    private readonly IFundFlowSource _fundFlowSource;
    private readonly IAnnouncementSource _announcementSource;
    private readonly IReportRenderer _renderer;
    private readonly IReportDelivery _delivery;
    private readonly ISummarizer _summarizer;
    private readonly AgentOptions _options;
    private readonly ILogger<ReportOrchestrator> _logger;

    public ReportOrchestrator(
        IQuoteSource quoteSource,
        IKlineSource klineSource,
        IFundFlowSource fundFlowSource,
        IAnnouncementSource announcementSource,
        IReportRenderer renderer,
        IReportDelivery delivery,
        ISummarizer summarizer,
        AgentOptions options,
        ILogger<ReportOrchestrator> logger)
    {
        _quoteSource = quoteSource;
        _klineSource = klineSource;
        _fundFlowSource = fundFlowSource;
        _announcementSource = announcementSource;
        _renderer = renderer;
        _delivery = delivery;
        _summarizer = summarizer;
        _options = options;
        _logger = logger;
    }

    public async Task<DailyReport> RunAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var analyses = new List<StockAnalysis>(_options.Stocks.Count);

        foreach (var stock in _options.Stocks)
        {
            var analysis = await AnalyzeStockAsync(stock, cancellationToken);
            if (analysis is not null)
            {
                analyses.Add(analysis);
            }
        }

        var report = new DailyReport
        {
            Date = date,
            Summary = PortfolioSummary.From(analyses),
            Stocks = analyses,
            RiskHits = CollectRiskHits(analyses),
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

        _logger.LogInformation("报告生成完成：{Count} 只股票，{Alerted} 只异动。",
            analyses.Count, report.Alerted.Count);

        return report;
    }

    private async Task<StockAnalysis?> AnalyzeStockAsync(StockConfig stock, CancellationToken ct)
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

            var analysis = StockAnalyzer.Analyze(quote, bars, _options.Alerts);
            return analysis with
            {
                FundFlow = await TryGetFundFlowAsync(code, ct),
                Announcements = await TryGetAnnouncementsAsync(code, ct),
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "拉取失败，跳过：{Code}", code);
            return null;
        }
    }

    /// <summary>
    /// 拉取资金流；资金流为补充信息，失败不应影响该股入报告，降级为 null。
    /// </summary>
    private async Task<FundFlow?> TryGetFundFlowAsync(StockCode code, CancellationToken ct)
    {
        try
        {
            return await _fundFlowSource.GetLatestFundFlowAsync(code, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "资金流获取失败，降级为空：{Code}", code);
            return null;
        }
    }

    /// <summary>
    /// 拉取并按重要类型过滤公告；公告为补充信息，失败降级为空列表。
    /// </summary>
    private async Task<IReadOnlyList<Announcement>> TryGetAnnouncementsAsync(
        StockCode code, CancellationToken ct)
    {
        try
        {
            var raw = await _announcementSource.GetRecentAnnouncementsAsync(
                code, _options.Announcements.Lookback, ct);
            return AnnouncementFilter.FilterImportant(raw, _options.Announcements);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "公告获取失败，降级为空：{Code}", code);
            return [];
        }
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
