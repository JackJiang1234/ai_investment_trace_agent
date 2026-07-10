using Agent.Core;

namespace Agent.DataSource;

/// <summary>
/// 基于东方财富 datacenter 的财报数据源：业绩报表（关键财务）+ 业绩预告。
/// A股覆盖；港股该接口无数据（返回 null）。
/// </summary>
public sealed class EastMoneyFinancialSource : IFinancialSource
{
    private const string BaseUrl = "https://datacenter-web.eastmoney.com/api/data/v1/get";
    private const int MaxAttempts = 2;

    private readonly HttpClient _httpClient;

    public EastMoneyFinancialSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<FinancialInfo?> GetFinancialsAsync(
        StockCode code, CancellationToken cancellationToken = default)
    {
        var filter = $"(SECURITY_CODE%3D%22{code.Symbol}%22)";

        var snapshotUrl = $"{BaseUrl}?reportName=RPT_LICO_FN_CPD&columns=ALL" +
                          $"&filter={filter}&pageSize=1&sortColumns=REPORTDATE&sortTypes=-1";
        var forecastUrl = $"{BaseUrl}?reportName=RPT_PUBLIC_OP_NEWPREDICT&columns=ALL" +
                          $"&filter={filter}&pageSize=1&sortColumns=NOTICE_DATE&sortTypes=-1";

        var snapshot = EastMoneyFinancialParser.ParseSnapshot(
            await GetWithRetryAsync(snapshotUrl, code, cancellationToken));
        var forecast = EastMoneyFinancialParser.ParseForecast(
            await GetWithRetryAsync(forecastUrl, code, cancellationToken));

        var info = new FinancialInfo { Snapshot = snapshot, Forecast = forecast };
        return info.HasAny ? info : null;
    }

    private async Task<string> GetWithRetryAsync(string url, StockCode code, CancellationToken ct)
    {
        HttpRequestException? lastError = null;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                return await _httpClient.GetStringAsync(url, ct);
            }
            catch (HttpRequestException ex)
            {
                lastError = ex;
            }
        }

        throw new HttpRequestException(
            $"获取财报失败（{code}），已重试 {MaxAttempts} 次。", lastError);
    }
}
