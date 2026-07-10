namespace Agent.Core;

/// <summary>财报数据源抽象。</summary>
public interface IFinancialSource
{
    /// <summary>
    /// 获取个股最新财报关键数据与业绩预告；均无则返回 null。
    /// 财报为补充信息，调用方应对 null 优雅降级。
    /// </summary>
    /// <exception cref="HttpRequestException">网络请求在重试后仍失败。</exception>
    Task<FinancialInfo?> GetFinancialsAsync(StockCode code, CancellationToken cancellationToken = default);
}
