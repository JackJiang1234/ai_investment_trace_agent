namespace Agent.Core;

/// <summary>
/// 持仓与买卖点计算（纯函数）。市值类一律折算为人民币，买卖点在人民币口径下判定。
/// 每股价格（现价/成本价）保留原币种，不折算。
/// </summary>
public static class HoldingCalculator
{
    private const decimal Yi = 100_000_000m;

    /// <summary>
    /// 由行情、配置、汇率与年初收盘价计算单只股票的持仓/买卖点指标。
    /// </summary>
    /// <param name="quote">当日行情（含原币种总市值）。</param>
    /// <param name="config">该股配置（持仓/买卖点，均可空）。</param>
    /// <param name="converter">人民币折算器。</param>
    /// <param name="yearStartClose">当年首个交易日收盘价（原币种）；无则 YTD 为 null。</param>
    public static HoldingMetrics Compute(
        Quote quote, StockConfig config, CurrencyConverter converter, decimal? yearStartClose)
    {
        var currency = quote.Code.Market.ToCurrency();

        // 当前市值：公司总市值折人民币后换算亿元；无数据（0）为 null。
        decimal? totalCapCnyYi = quote.TotalMarketCap > 0
            ? converter.ToCny(quote.TotalMarketCap, currency) / Yi
            : null;

        // 持仓市值：现价 × 份数，折人民币；无份数为 null。
        decimal? holdingValueCny = config.Shares is { } shares
            ? converter.ToCny(quote.Price * shares, currency)
            : null;

        // 年初持仓市值：年初收盘 × 份数，折人民币；用于组合年度收益。需同时有份数与年初收盘。
        decimal? yearStartValueCny = config.Shares is { } sh && yearStartClose is { } ysClose && ysClose > 0
            ? converter.ToCny(ysClose * sh, currency)
            : null;

        decimal? ytd = yearStartClose is { } baseClose && baseClose > 0
            ? (quote.Price - baseClose) / baseClose * 100m
            : null;

        var canBuy = config.IdealBuyMarketCapYi is { } buy
                     && totalCapCnyYi is { } capB && capB < buy;
        var shouldSell = config.SellMarketCapYi is { } sell
                         && totalCapCnyYi is { } capS && capS > sell;

        return new HoldingMetrics
        {
            Group = config.Group,
            Currency = currency,
            Shares = config.Shares,
            CostPrice = config.CostPrice,
            TotalMarketCapCnyYi = totalCapCnyYi,
            HoldingValueCny = holdingValueCny,
            YearStartValueCny = yearStartValueCny,
            IdealBuyYi = config.IdealBuyMarketCapYi,
            SellYi = config.SellMarketCapYi,
            YtdReturnPercent = ytd,
            CanBuy = canBuy,
            ShouldSell = shouldSell,
        };
    }
}
