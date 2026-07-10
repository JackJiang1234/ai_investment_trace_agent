namespace Agent.Core;

/// <summary>单只股票当日的完整分析结果：行情 + 技术指标 + 命中的异动提醒。</summary>
public sealed record StockAnalysis
{
    /// <summary>当日行情快照。</summary>
    public required Quote Quote { get; init; }

    /// <summary>5 日均线；数据不足为 null。</summary>
    public decimal? Ma5 { get; init; }

    /// <summary>20 日均线；数据不足为 null。</summary>
    public decimal? Ma20 { get; init; }

    /// <summary>60 日均线；数据不足为 null。</summary>
    public decimal? Ma60 { get; init; }

    /// <summary>近 5 日涨跌幅（%）；数据不足为 null。</summary>
    public decimal? Return5 { get; init; }

    /// <summary>近 20 日涨跌幅（%）；数据不足为 null。</summary>
    public decimal? Return20 { get; init; }

    /// <summary>是否创近 20 日新高。</summary>
    public bool IsNewHigh20 { get; init; }

    /// <summary>是否创近 20 日新低。</summary>
    public bool IsNewLow20 { get; init; }

    /// <summary>当日资金流向；无数据或获取失败为 null。</summary>
    public FundFlow? FundFlow { get; init; }

    /// <summary>经重要类型过滤后的近期公告。</summary>
    public IReadOnlyList<Announcement> Announcements { get; init; } = [];

    /// <summary>财报信息（关键财务数据 + 业绩预告）；无数据或获取失败为 null。</summary>
    public FinancialInfo? Financials { get; init; }

    /// <summary>命中的异动提醒。</summary>
    public IReadOnlyList<TriggeredAlert> Alerts { get; init; } = [];

    /// <summary>是否存在任一异动。</summary>
    public bool HasAlert => Alerts.Count > 0;
}
