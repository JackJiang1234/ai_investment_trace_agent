namespace Agent.Core;

/// <summary>某一交易日的完整追踪报告数据模型（渲染前的中间产物）。</summary>
public sealed record DailyReport
{
    /// <summary>报告日期。</summary>
    public required DateOnly Date { get; init; }

    /// <summary>组合概览。</summary>
    public required PortfolioSummary Summary { get; init; }

    /// <summary>全部个股分析（按配置顺序）。</summary>
    public required IReadOnlyList<StockAnalysis> Stocks { get; init; }

    /// <summary>命中异动的个股（置顶展示用）。</summary>
    public IReadOnlyList<StockAnalysis> Alerted =>
        Stocks.Where(s => s.HasAlert).ToArray();

    /// <summary>AI 摘要（默认关闭时为 null）。</summary>
    public string? AiSummary { get; init; }
}
