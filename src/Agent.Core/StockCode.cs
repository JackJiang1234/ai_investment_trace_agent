namespace Agent.Core;

/// <summary>
/// 规范化的股票代码，形如 <c>600519.SH</c> / <c>000001.SZ</c> / <c>00700.HK</c>。
/// 负责在系统边界解析市场与纯代码，并提供各数据源所需的标识（如东财 secid）。
/// </summary>
public sealed record StockCode
{
    private StockCode(Market market, string symbol)
    {
        Market = market;
        Symbol = symbol;
    }

    /// <summary>所属市场。</summary>
    public Market Market { get; }

    /// <summary>不含市场后缀的纯代码，如 <c>600519</c>。</summary>
    public string Symbol { get; }

    /// <summary>
    /// 东方财富 push2 接口所需的 secid，格式 <c>{marketPrefix}.{symbol}</c>。
    /// 市场前缀：上证=1、深证=0、港股=116。
    /// </summary>
    public string EastMoneySecId => $"{EastMoneyPrefix(Market)}.{Symbol}";

    /// <summary>
    /// 解析形如 <c>600519.SH</c> 的代码。大小写不敏感，允许首尾空白。
    /// </summary>
    /// <exception cref="FormatException">输入为空、缺少代码或市场、或市场后缀无法识别。</exception>
    public static StockCode Parse(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new FormatException("股票代码不能为空。");
        }

        var parts = code.Trim().Split('.');
        if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
        {
            throw new FormatException($"股票代码格式非法：'{code}'，应形如 '600519.SH'。");
        }

        var symbol = parts[0];
        if (!Enum.TryParse<Market>(parts[1], ignoreCase: true, out var market))
        {
            throw new FormatException($"无法识别的市场后缀：'{parts[1]}'（支持 SH/SZ/HK）。");
        }

        return new StockCode(market, symbol);
    }

    private static string EastMoneyPrefix(Market market) => market switch
    {
        Market.SH => "1",
        Market.SZ => "0",
        Market.HK => "116",
        _ => throw new ArgumentOutOfRangeException(nameof(market), market, "未知市场。"),
    };

    /// <summary>返回规范化的代码字符串，如 <c>600519.SH</c>。</summary>
    public override string ToString() => $"{Symbol}.{Market}";
}
