using Agent.Core;

namespace Agent.DataSource;

/// <summary>
/// 基于东方财富公告接口的数据源。
/// A股（沪/深）覆盖良好；港股该接口暂不返回数据（返回空），港股公告将在后续里程碑经披露易补齐。
/// </summary>
public sealed class EastMoneyAnnouncementSource : IAnnouncementSource
{
    private const string BaseUrl = "https://np-anotice-stock.eastmoney.com/api/security/ann";
    private const int MaxAttempts = 2;

    private readonly HttpClient _httpClient;

    public EastMoneyAnnouncementSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<Announcement>> GetRecentAnnouncementsAsync(
        StockCode code, int count, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        var annType = code.Market == Market.HK ? "HK" : "A";
        var url = $"{BaseUrl}?sr=-1&page_size={count}&page_index=1" +
                  $"&ann_type={annType}&client_source=web&stock_list={code.Symbol}";

        HttpRequestException? lastError = null;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var json = await _httpClient.GetStringAsync(url, cancellationToken);
                return EastMoneyAnnouncementParser.Parse(json);
            }
            catch (HttpRequestException ex)
            {
                lastError = ex;
            }
        }

        throw new HttpRequestException(
            $"获取公告失败（{code}），已重试 {MaxAttempts} 次。", lastError);
    }
}
