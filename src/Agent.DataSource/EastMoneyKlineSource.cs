using Agent.Core;

namespace Agent.DataSource;

/// <summary>
/// 基于东方财富 push2his 日K线接口的历史行情数据源。
/// 通过注入的 <see cref="HttpClient"/> 请求，交给 <see cref="EastMoneyKlineParser"/> 解析。
/// </summary>
public sealed class EastMoneyKlineSource : IKlineSource
{
    private const string BaseUrl = "https://push2his.eastmoney.com/api/qt/stock/kline/get";
    private const string Fields1 = "f1,f2,f3,f4,f5,f6";
    // 列序：日期,开,收,高,低,量,额,振幅,涨跌幅,涨跌额,换手率
    private const string Fields2 = "f51,f52,f53,f54,f55,f56,f57,f58,f59,f60,f61";
    private const string EndSentinel = "20500101"; // 取到最新交易日
    private const int Klt = 101; // 日K
    private const int Fqt = 1;   // 前复权
    private const int MaxAttempts = 2;

    private readonly HttpClient _httpClient;

    public EastMoneyKlineSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<DailyBar>> GetDailyBarsAsync(
        StockCode code, int count, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        var url = $"{BaseUrl}?secid={code.EastMoneySecId}" +
                  $"&fields1={Fields1}&fields2={Fields2}" +
                  $"&klt={Klt}&fqt={Fqt}&end={EndSentinel}&lmt={count}";

        return await FetchBarsAsync(code, url, cancellationToken);
    }

    public async Task<decimal?> GetYearStartCloseAsync(
        StockCode code, int year, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(year);

        // 从年初起按日期升序取，首根即当年首个交易日；lmt 留足覆盖整年。
        var url = $"{BaseUrl}?secid={code.EastMoneySecId}" +
                  $"&fields1={Fields1}&fields2={Fields2}" +
                  $"&klt={Klt}&fqt={Fqt}&beg={year:D4}0101&end={EndSentinel}&lmt=400";

        var bars = await FetchBarsAsync(code, url, cancellationToken);
        return bars.Count > 0 ? bars[0].Close : null;
    }

    private async Task<IReadOnlyList<DailyBar>> FetchBarsAsync(
        StockCode code, string url, CancellationToken cancellationToken)
    {
        HttpRequestException? lastError = null;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var json = await _httpClient.GetStringAsync(url, cancellationToken);
                return EastMoneyKlineParser.Parse(json);
            }
            catch (HttpRequestException ex)
            {
                lastError = ex;
            }
        }

        throw new HttpRequestException(
            $"获取K线失败（{code}），已重试 {MaxAttempts} 次。", lastError);
    }
}
