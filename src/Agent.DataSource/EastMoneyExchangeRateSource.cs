using Agent.Core;

namespace Agent.DataSource;

/// <summary>
/// 基于东方财富 push2 外汇接口的汇率源。港元兑离岸人民币 <c>133.HKDCNH</c>（CNH≈CNY，够用）。
/// </summary>
public sealed class EastMoneyExchangeRateSource : IExchangeRateSource
{
    private const string Url =
        "https://push2.eastmoney.com/api/qt/stock/get?secid=133.HKDCNH&fields=f43,f58,f59";
    private const int MaxAttempts = 2;

    private readonly HttpClient _httpClient;

    public EastMoneyExchangeRateSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<decimal?> GetHkdToCnyAsync(CancellationToken cancellationToken = default)
    {
        HttpRequestException? lastError = null;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var json = await _httpClient.GetStringAsync(Url, cancellationToken);
                return ExchangeRateParser.Parse(json);
            }
            catch (HttpRequestException ex)
            {
                lastError = ex;
            }
        }

        throw new HttpRequestException(
            $"获取汇率失败（HKDCNH），已重试 {MaxAttempts} 次。", lastError);
    }
}
