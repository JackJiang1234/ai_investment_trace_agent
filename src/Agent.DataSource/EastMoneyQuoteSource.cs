using Agent.Core;

namespace Agent.DataSource;

/// <summary>
/// 基于东方财富 push2 接口的行情数据源。
/// 通过注入的 <see cref="HttpClient"/> 发起请求，交给 <see cref="EastMoneyQuoteParser"/> 解析。
/// </summary>
public sealed class EastMoneyQuoteSource : IQuoteSource
{
    // push2 行情接口。fields 见 EastMoneyQuoteParser 的字段说明。
    private const string BaseUrl = "https://push2.eastmoney.com/api/qt/stock/get";
    private const string Fields = "f43,f44,f45,f46,f47,f48,f50,f57,f58,f59,f60,f116,f168,f169,f170";
    private const int MaxAttempts = 2; // 首次 + 1 次重试

    private readonly HttpClient _httpClient;

    public EastMoneyQuoteSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Quote?> GetQuoteAsync(StockCode code, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}?secid={code.EastMoneySecId}&fields={Fields}";

        HttpRequestException? lastError = null;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var json = await _httpClient.GetStringAsync(url, cancellationToken);
                return EastMoneyQuoteParser.Parse(json, code);
            }
            catch (HttpRequestException ex)
            {
                lastError = ex;
            }
        }

        throw new HttpRequestException(
            $"获取行情失败（{code}），已重试 {MaxAttempts} 次。", lastError);
    }
}
