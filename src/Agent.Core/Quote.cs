namespace Agent.Core;

/// <summary>
/// 单只股票某一时点的行情快照（收盘后取值即为当日收盘数据）。
/// 金额/价格使用 <see cref="decimal"/> 以保证精度。
/// </summary>
public sealed record Quote
{
    /// <summary>股票代码。</summary>
    public required StockCode Code { get; init; }

    /// <summary>股票名称，如"贵州茅台"。</summary>
    public required string Name { get; init; }

    /// <summary>现价（收盘后为收盘价）。</summary>
    public required decimal Price { get; init; }

    /// <summary>昨收价。</summary>
    public required decimal PreviousClose { get; init; }

    /// <summary>今开价。</summary>
    public required decimal Open { get; init; }

    /// <summary>最高价。</summary>
    public required decimal High { get; init; }

    /// <summary>最低价。</summary>
    public required decimal Low { get; init; }

    /// <summary>涨跌额。</summary>
    public required decimal ChangeAmount { get; init; }

    /// <summary>涨跌幅（百分比数值，如 -1.5 表示 -1.5%）。</summary>
    public required decimal ChangePercent { get; init; }

    /// <summary>成交量（原始单位：A股为手）。</summary>
    public required long Volume { get; init; }

    /// <summary>成交额（原始单位：元/港元）。</summary>
    public required decimal Turnover { get; init; }

    /// <summary>换手率（百分比数值）。</summary>
    public required decimal TurnoverRate { get; init; }

    /// <summary>量比。</summary>
    public required decimal VolumeRatio { get; init; }

    /// <summary>
    /// 公司总市值（原始货币金额，A股为元、港股为港元；东财 push2 <c>f116</c>，不缩放）。
    /// 无数据时为 0。换算亿元 ÷1e8；港股需再折算人民币。
    /// </summary>
    public decimal TotalMarketCap { get; init; }
}
