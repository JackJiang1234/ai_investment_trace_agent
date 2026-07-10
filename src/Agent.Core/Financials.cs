namespace Agent.Core;

/// <summary>一期财报的关键财务数据。金额单位：元。</summary>
public sealed record FinancialSnapshot
{
    /// <summary>报告期（如 2026-03-31）。</summary>
    public required DateOnly ReportPeriod { get; init; }

    /// <summary>报告期标签（如 "2026年 一季报"）。</summary>
    public required string PeriodLabel { get; init; }

    /// <summary>披露日期。</summary>
    public required DateOnly NoticeDate { get; init; }

    /// <summary>营业总收入（元）。</summary>
    public required decimal Revenue { get; init; }

    /// <summary>归母净利润（元）。</summary>
    public required decimal NetProfit { get; init; }

    /// <summary>营收同比（%）。</summary>
    public decimal? RevenueYoY { get; init; }

    /// <summary>净利同比（%）。</summary>
    public decimal? NetProfitYoY { get; init; }

    /// <summary>加权净资产收益率（%）。</summary>
    public decimal? Roe { get; init; }

    /// <summary>销售毛利率（%）。</summary>
    public decimal? GrossMargin { get; init; }

    /// <summary>基本每股收益（元）。</summary>
    public decimal? Eps { get; init; }

    /// <summary>营业收入（亿元），供展示。</summary>
    public decimal RevenueInYi => Revenue / 100_000_000m;

    /// <summary>归母净利润（亿元），供展示。</summary>
    public decimal NetProfitInYi => NetProfit / 100_000_000m;
}

/// <summary>业绩预告（先行信号）。</summary>
public sealed record EarningsForecast
{
    /// <summary>披露日期。</summary>
    public required DateOnly NoticeDate { get; init; }

    /// <summary>报告期。</summary>
    public required DateOnly ReportPeriod { get; init; }

    /// <summary>预告类型（如 略增/预增/预减/首亏/续亏）。</summary>
    public required string Type { get; init; }

    /// <summary>预告内容（自然语言描述）。</summary>
    public required string Content { get; init; }

    /// <summary>ISO 披露日期文本，供展示（避免区域设置影响）。</summary>
    public string NoticeDateText =>
        NoticeDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>个股财报信息汇总：最新财报 + 业绩预告。</summary>
public sealed record FinancialInfo
{
    /// <summary>最新一期财报关键数据；无则 null。</summary>
    public FinancialSnapshot? Snapshot { get; init; }

    /// <summary>最新业绩预告；无则 null。</summary>
    public EarningsForecast? Forecast { get; init; }

    /// <summary>是否有任一财报信息。</summary>
    public bool HasAny => Snapshot is not null || Forecast is not null;
}
