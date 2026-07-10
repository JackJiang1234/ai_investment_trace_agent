namespace Agent.Core;

/// <summary>
/// 组合概览（v1.1，估值/持仓口径）：持仓股票数、命中买点/卖点家数、组合总持仓市值（元人民币）。
/// </summary>
public sealed record PortfolioSummary
{
    /// <summary>有持仓的股票数（持股份数非空）。</summary>
    public required int HoldingCount { get; init; }

    /// <summary>命中理想买点（可以击球）的股票数（跨分组）。</summary>
    public required int BuySignalCount { get; init; }

    /// <summary>命中卖点的股票数（跨分组）。</summary>
    public required int SellSignalCount { get; init; }

    /// <summary>组合总持仓市值（元人民币）= 各有持仓股票的持仓市值之和。</summary>
    public required decimal TotalHoldingValueCny { get; init; }

    /// <summary>由个股分析汇总组合概览。</summary>
    public static PortfolioSummary From(IReadOnlyList<StockAnalysis> stocks)
    {
        var holdingCount = 0;
        var buy = 0;
        var sell = 0;
        decimal total = 0m;

        foreach (var s in stocks)
        {
            var h = s.Holding;
            if (h is null)
            {
                continue;
            }

            if (h.HoldingValueCny is { } value)
            {
                holdingCount++;
                total += value;
            }

            if (h.CanBuy) buy++;
            if (h.ShouldSell) sell++;
        }

        return new PortfolioSummary
        {
            HoldingCount = holdingCount,
            BuySignalCount = buy,
            SellSignalCount = sell,
            TotalHoldingValueCny = total,
        };
    }
}
