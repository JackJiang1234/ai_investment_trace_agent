using Agent.Core;

namespace Agent.DataSource;

/// <summary>
/// 按市场分派公告数据源：A股（沪/深）走东财统一公告 API，港股走披露易 hkexnews。
/// </summary>
public sealed class RoutingAnnouncementSource : IAnnouncementSource
{
    private readonly EastMoneyAnnouncementSource _aShare;
    private readonly HkexnewsAnnouncementSource _hongKong;

    public RoutingAnnouncementSource(
        EastMoneyAnnouncementSource aShare, HkexnewsAnnouncementSource hongKong)
    {
        _aShare = aShare;
        _hongKong = hongKong;
    }

    public Task<IReadOnlyList<Announcement>> GetRecentAnnouncementsAsync(
        StockCode code, int count, CancellationToken cancellationToken = default)
    {
        var source = code.Market == Market.HK ? (IAnnouncementSource)_hongKong : _aShare;
        return source.GetRecentAnnouncementsAsync(code, count, cancellationToken);
    }
}
