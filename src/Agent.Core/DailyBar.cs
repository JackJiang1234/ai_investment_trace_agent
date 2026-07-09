namespace Agent.Core;

/// <summary>
/// 单个交易日的日K线数据。价格为不复权/前复权后的可读值（无需缩放）。
/// </summary>
public sealed record DailyBar
{
    /// <summary>交易日期。</summary>
    public required DateOnly Date { get; init; }

    /// <summary>开盘价。</summary>
    public required decimal Open { get; init; }

    /// <summary>收盘价。</summary>
    public required decimal Close { get; init; }

    /// <summary>最高价。</summary>
    public required decimal High { get; init; }

    /// <summary>最低价。</summary>
    public required decimal Low { get; init; }

    /// <summary>成交量（原始单位：A股为手）。</summary>
    public required long Volume { get; init; }

    /// <summary>成交额（元/港元）。</summary>
    public required decimal Amount { get; init; }

    /// <summary>涨跌幅（百分比数值，如 -1.5 表示 -1.5%）。</summary>
    public required decimal ChangePercent { get; init; }

    /// <summary>换手率（百分比数值）。</summary>
    public required decimal TurnoverRate { get; init; }
}
