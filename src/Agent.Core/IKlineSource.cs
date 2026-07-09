namespace Agent.Core;

/// <summary>历史日K线数据源抽象。</summary>
public interface IKlineSource
{
    /// <summary>
    /// 获取最近 <paramref name="count"/> 个交易日的日K线，按日期升序返回（最早在前）。
    /// 无数据时返回空列表。
    /// </summary>
    /// <exception cref="HttpRequestException">网络请求在重试后仍失败。</exception>
    Task<IReadOnlyList<DailyBar>> GetDailyBarsAsync(
        StockCode code, int count, CancellationToken cancellationToken = default);
}
