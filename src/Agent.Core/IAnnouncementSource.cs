namespace Agent.Core;

/// <summary>公司公告数据源抽象。</summary>
public interface IAnnouncementSource
{
    /// <summary>
    /// 获取最近 <paramref name="count"/> 条公告，按日期降序（最新在前）。无数据返回空列表。
    /// 公告为补充信息，调用方应对空/异常优雅降级。
    /// </summary>
    /// <exception cref="HttpRequestException">网络请求在重试后仍失败。</exception>
    Task<IReadOnlyList<Announcement>> GetRecentAnnouncementsAsync(
        StockCode code, int count, CancellationToken cancellationToken = default);
}
