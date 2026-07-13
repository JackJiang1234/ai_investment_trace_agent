namespace Agent.Core;

/// <summary>
/// 单只股票的持仓与买卖点指标（v1.1）。市值类字段均已折算为人民币口径。
/// 金额单位：<see cref="TotalMarketCapCnyYi"/> 为亿元人民币，<see cref="HoldingValueCny"/> 为元人民币。
/// </summary>
public sealed record HoldingMetrics
{
    /// <summary>分组标签（核心持仓/观察池等）。</summary>
    public string? Group { get; init; }

    /// <summary>该股计价货币（港股 HKD、A股 CNY）。每股价格按此币种展示。</summary>
    public required Currency Currency { get; init; }

    /// <summary>持股份数；观察池为 null。</summary>
    public long? Shares { get; init; }

    /// <summary>成本价（每股，原币种）；观察池为 null。</summary>
    public decimal? CostPrice { get; init; }

    /// <summary>当前市值：公司总市值（亿元人民币）；无总市值数据为 null。</summary>
    public decimal? TotalMarketCapCnyYi { get; init; }

    /// <summary>持仓市值（元人民币）= 现价 × 持股份数（折人民币）；无持股为 null。</summary>
    public decimal? HoldingValueCny { get; init; }

    /// <summary>年初持仓市值（元人民币）= 年初首个交易日收盘 × 持股份数（折人民币）；无持股或无年初收盘为 null。</summary>
    public decimal? YearStartValueCny { get; init; }

    /// <summary>成本市值（元人民币）= 成本价 × 持股份数（折人民币）；无持股或无成本价为 null。</summary>
    public decimal? CostValueCny { get; init; }

    /// <summary>对成本盈亏金额（元人民币）=（现价 − 成本价）× 份数（折人民币）；无持股或无成本价为 null。</summary>
    public decimal? CostProfitCny { get; init; }

    /// <summary>对成本收益率（%，原币种，不受汇率影响）=（现价 − 成本价）÷ 成本价；无成本价或成本价≤0 为 null。</summary>
    public decimal? CostReturnPercent { get; init; }

    /// <summary>理想买点（亿元人民币）。</summary>
    public decimal? IdealBuyYi { get; init; }

    /// <summary>1 年内卖点（亿元人民币）。</summary>
    public decimal? SellYi { get; init; }

    /// <summary>年内涨幅（%）；无年初收盘为 null。</summary>
    public decimal? YtdReturnPercent { get; init; }

    /// <summary>是否命中理想买点（当前总市值 &lt; 理想买点）→ 可以击球。</summary>
    public bool CanBuy { get; init; }

    /// <summary>是否命中卖点（当前总市值 &gt; 1年内卖点）。</summary>
    public bool ShouldSell { get; init; }

    /// <summary>组合内权重（%）= 该股持仓市值 ÷ 全部持仓总市值；由编排层跨股汇总后回填。</summary>
    public decimal? HoldingRatioPercent { get; init; }

    /// <summary>每股价格的币种符号（港股 HK$、A股 ¥），供报告展示。</summary>
    public string CurrencySymbol => Currency == Currency.HKD ? "HK$" : "¥";

    /// <summary>是否为持仓股票（有持股份数）；观察池为 false，卡片精简。</summary>
    public bool IsHolding => Shares.HasValue;
}
