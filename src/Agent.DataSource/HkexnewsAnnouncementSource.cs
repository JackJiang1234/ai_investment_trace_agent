using Agent.Core;

namespace Agent.DataSource;

/// <summary>
/// 港股公告数据源，基于香港交易所披露易（hkexnews）。
/// 两步：prefix.do 用 ticker 查内部 stockId → titleSearchServlet 检索公告。
/// </summary>
public sealed class HkexnewsAnnouncementSource : IAnnouncementSource
{
    private const string PrefixUrl = "https://www1.hkexnews.hk/search/prefix.do";
    private const string SearchUrl = "https://www1.hkexnews.hk/search/titleSearchServlet.do";
    private const int MaxAttempts = 2;

    private readonly HttpClient _httpClient;

    public HkexnewsAnnouncementSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<Announcement>> GetRecentAnnouncementsAsync(
        StockCode code, int count, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        var stockId = await LookupStockIdAsync(code, cancellationToken);
        if (stockId is null)
        {
            return [];
        }

        var searchUrl = $"{SearchUrl}?sortDir=0&sortByOptions=DateTime&category=0&market=SEHK" +
                        $"&stockId={stockId}&documentType=-1&fromDate=&toDate=&title=" +
                        $"&searchType=1&t=1&lang=en";

        var json = await GetWithRetryAsync(searchUrl, code, cancellationToken);
        var all = HkexnewsAnnouncementParser.Parse(json);
        return all.Count > count ? all.Take(count).ToArray() : all;
    }

    private async Task<int?> LookupStockIdAsync(StockCode code, CancellationToken ct)
    {
        var url = $"{PrefixUrl}?callback=c&lang=EN&type=A&name={code.Symbol}&market=SEHK";
        var jsonp = await GetWithRetryAsync(url, code, ct);
        return HkexnewsAnnouncementParser.ParseStockId(jsonp);
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
            $"获取港股公告失败（{code}），已重试 {MaxAttempts} 次。", lastError);
    }
}
