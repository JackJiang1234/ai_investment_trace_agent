using Agent.Core;

namespace Agent.DataSource;

/// <summary>
/// 基于东方财富 push2his 资金流向接口的数据源，返回最新交易日的资金流。
/// </summary>
public sealed class EastMoneyFundFlowSource : IFundFlowSource
{
    private const string BaseUrl = "https://push2his.eastmoney.com/api/qt/stock/fflow/kline/get";
    private const string Fields1 = "f1,f2,f3,f7";
    // 列序：日期,主力,小单,中单,大单,超大单
    private const string Fields2 = "f51,f52,f53,f54,f55,f56,f57,f58,f59,f60,f61,f62,f63,f64,f65";
    private const int Klt = 101; // 日频
    private const int Lmt = 5;   // 取最近数日，用最后一日
    private const int MaxAttempts = 2;

    private readonly HttpClient _httpClient;

    public EastMoneyFundFlowSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<FundFlow?> GetLatestFundFlowAsync(
        StockCode code, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}?secid={code.EastMoneySecId}" +
                  $"&fields1={Fields1}&fields2={Fields2}&klt={Klt}&lmt={Lmt}";

        HttpRequestException? lastError = null;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var json = await _httpClient.GetStringAsync(url, cancellationToken);
                var flows = EastMoneyFundFlowParser.Parse(json);
                return flows.Count > 0 ? flows[^1] : null;
            }
            catch (HttpRequestException ex)
            {
                lastError = ex;
            }
        }

        throw new HttpRequestException(
            $"获取资金流失败（{code}），已重试 {MaxAttempts} 次。", lastError);
    }
}
