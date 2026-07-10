namespace Agent.Core;

/// <summary>汇率数据源抽象（本期仅需港元兑人民币）。</summary>
public interface IExchangeRateSource
{
    /// <summary>
    /// 获取 1 港元折人民币的汇率；无数据或响应异常时返回 <see langword="null"/>，
    /// 调用方应回退到配置的静态汇率。
    /// </summary>
    /// <exception cref="HttpRequestException">网络请求在重试后仍失败。</exception>
    Task<decimal?> GetHkdToCnyAsync(CancellationToken cancellationToken = default);
}
