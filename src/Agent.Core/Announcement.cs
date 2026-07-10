namespace Agent.Core;

/// <summary>一条公司公告。</summary>
public sealed record Announcement
{
    /// <summary>公告日期。</summary>
    public required DateOnly Date { get; init; }

    /// <summary>公告标题。</summary>
    public required string Title { get; init; }

    /// <summary>公告类型（可能由多个分类合并，以 / 分隔）。</summary>
    public required string Type { get; init; }

    /// <summary>原文链接。</summary>
    public required string Url { get; init; }

    /// <summary>ISO 日期文本，供报告展示（避免受运行环境区域设置影响）。</summary>
    public string DateText =>
        Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>命中风险关键词的公告及其所属个股上下文（用于报告风险提示区）。</summary>
public sealed record RiskHit
{
    /// <summary>所属股票代码。</summary>
    public required StockCode Code { get; init; }

    /// <summary>股票名称。</summary>
    public required string StockName { get; init; }

    /// <summary>命中的公告。</summary>
    public required Announcement Announcement { get; init; }

    /// <summary>命中的风险关键词。</summary>
    public required string MatchedKeyword { get; init; }
}
