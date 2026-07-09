namespace Agent.Core;

/// <summary>资金流向数据源抽象。</summary>
public interface IFundFlowSource
{
    /// <summary>
    /// 获取最新交易日的资金流向；无数据时返回 <see langword="null"/>。
    /// 资金流为补充信息，调用方应对 null 优雅降级。
    /// </summary>
    /// <exception cref="HttpRequestException">网络请求在重试后仍失败。</exception>
    Task<FundFlow?> GetLatestFundFlowAsync(StockCode code, CancellationToken cancellationToken = default);
}
