namespace Agent.Core;

/// <summary>单只股票的分析结果：行情 + 近期公告 + 财报。（v1.1 移除技术指标/异动/资金流）</summary>
public sealed record StockAnalysis
{
    /// <summary>当日行情快照。</summary>
    public required Quote Quote { get; init; }

    /// <summary>持仓与买卖点指标（人民币口径）；由编排层计算填入。</summary>
    public HoldingMetrics? Holding { get; init; }

    /// <summary>经重要类型过滤后的近期公告。</summary>
    public IReadOnlyList<Announcement> Announcements { get; init; } = [];

    /// <summary>财报信息（关键财务数据 + 业绩预告）；无数据或获取失败为 null。</summary>
    public FinancialInfo? Financials { get; init; }
}
