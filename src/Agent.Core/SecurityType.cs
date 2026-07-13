namespace Agent.Core;

/// <summary>证券类型。买卖点口径不同：股票走公司总市值阈值，ETF 走单位价格阈值。</summary>
public enum SecurityType
{
    /// <summary>普通股票（买卖点按公司总市值，f116）。</summary>
    Stock,

    /// <summary>交易所基金 ETF/LOF（f116 为基金规模，买卖点按单位价格）。</summary>
    Etf,
}

/// <summary>证券类型识别：优先显式配置，否则按代码前缀推断。</summary>
public static class SecurityTypeResolver
{
    /// <summary>
    /// 按代码前缀推断类型：沪市 51/56/58 开头、深市 15/16 开头为 ETF/LOF，其余为股票。
    /// 港股一律按股票处理（本期不区分港股 ETF）。
    /// </summary>
    public static SecurityType Detect(StockCode code)
    {
        var s = code.Symbol;
        var isEtf = code.Market switch
        {
            Market.SH => s.StartsWith("51", StringComparison.Ordinal)
                         || s.StartsWith("56", StringComparison.Ordinal)
                         || s.StartsWith("58", StringComparison.Ordinal),
            Market.SZ => s.StartsWith("15", StringComparison.Ordinal)
                         || s.StartsWith("16", StringComparison.Ordinal),
            _ => false,
        };

        return isEtf ? SecurityType.Etf : SecurityType.Stock;
    }

    /// <summary>该配置的有效类型：显式 <see cref="StockConfig.SecurityType"/> 优先，否则按代码推断。</summary>
    public static SecurityType Resolve(StockConfig config, StockCode code) =>
        config.SecurityType ?? Detect(code);
}
