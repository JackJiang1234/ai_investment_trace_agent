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

    /// <summary>年度收益金额（元人民币，年内 YTD）= Σ(现价−年初收盘)×份数（折人民币）。</summary>
    public required decimal YtdProfitCny { get; init; }

    /// <summary>年度收益率（%，年内 YTD）= 年度收益金额 ÷ 年初持仓总市值；无年初市值时为 null。</summary>
    public decimal? YtdReturnPercent { get; init; }

    /// <summary>持仓总收益金额（元人民币，对成本）= Σ(现价−成本价)×份数（折人民币）。</summary>
    public required decimal CostProfitCny { get; init; }

    /// <summary>持仓总收益率（%，对成本）= 持仓总收益金额 ÷ 总成本市值；无成本市值时为 null。</summary>
    public decimal? CostReturnPercent { get; init; }

    /// <summary>由个股分析汇总组合概览。</summary>
    public static PortfolioSummary From(IReadOnlyList<StockAnalysis> stocks)
    {
        var holdingCount = 0;
        var buy = 0;
        var sell = 0;
        decimal total = 0m;
        decimal yearStartTotal = 0m;
        decimal ytdProfit = 0m;
        decimal costTotal = 0m;
        decimal costProfit = 0m;

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

                // 年度收益仅在同时有当前与年初持仓市值时纳入（缺年初收盘的持仓不计）。
                if (h.YearStartValueCny is { } yearStart)
                {
                    yearStartTotal += yearStart;
                    ytdProfit += value - yearStart;
                }
            }

            // 对成本收益仅在有成本市值时纳入（缺成本价的持仓不计）。
            if (h.CostValueCny is { } costValue && h.CostProfitCny is { } costGain)
            {
                costTotal += costValue;
                costProfit += costGain;
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
            YtdProfitCny = ytdProfit,
            YtdReturnPercent = yearStartTotal > 0m ? ytdProfit / yearStartTotal * 100m : null,
            CostProfitCny = costProfit,
            CostReturnPercent = costTotal > 0m ? costProfit / costTotal * 100m : null,
        };
    }
}
