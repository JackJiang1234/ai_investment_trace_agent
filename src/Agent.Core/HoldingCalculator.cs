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
        var securityType = SecurityTypeResolver.Resolve(config, quote.Code);

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

        // 对成本：成本市值与盈亏金额折人民币（需份数+成本价）；收益率按原币种（不受汇率影响）。
        var hasCostBasis = config.Shares is { } shForCost && config.CostPrice is { } cpForValue;
        decimal? costValueCny = hasCostBasis
            ? converter.ToCny(config.CostPrice!.Value * config.Shares!.Value, currency)
            : null;
        decimal? costProfitCny = hasCostBasis
            ? converter.ToCny((quote.Price - config.CostPrice!.Value) * config.Shares!.Value, currency)
            : null;
        decimal? costReturnPct = config.CostPrice is { } cp && cp > 0
            ? (quote.Price - cp) / cp * 100m
            : null;

        decimal? ytd = yearStartClose is { } baseClose && baseClose > 0
            ? (quote.Price - baseClose) / baseClose * 100m
            : null;

        // 买卖点按类型分口径：股票比公司总市值(折人民币)，ETF 比单位价格(原币种)。
        bool canBuy, shouldSell;
        if (securityType == SecurityType.Etf)
        {
            canBuy = config.IdealBuyPrice is { } bp && quote.Price < bp;
            shouldSell = config.SellPrice is { } sp && quote.Price > sp;
        }
        else
        {
            canBuy = config.IdealBuyMarketCapYi is { } buy
                     && totalCapCnyYi is { } capB && capB < buy;
            shouldSell = config.SellMarketCapYi is { } sell
                         && totalCapCnyYi is { } capS && capS > sell;
        }

        return new HoldingMetrics
        {
            Group = config.Group,
            SecurityType = securityType,
            Currency = currency,
            Shares = config.Shares,
            CostPrice = config.CostPrice,
            TotalMarketCapCnyYi = totalCapCnyYi,
            HoldingValueCny = holdingValueCny,
            YearStartValueCny = yearStartValueCny,
            CostValueCny = costValueCny,
            CostProfitCny = costProfitCny,
            CostReturnPercent = costReturnPct,
            IdealBuyYi = config.IdealBuyMarketCapYi,
            SellYi = config.SellMarketCapYi,
            IdealBuyPrice = config.IdealBuyPrice,
            SellPrice = config.SellPrice,
            YtdReturnPercent = ytd,
            CanBuy = canBuy,
            ShouldSell = shouldSell,
        };
    }
}
