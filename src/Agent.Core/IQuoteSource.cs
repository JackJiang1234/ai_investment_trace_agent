namespace Agent.Core;

/// <summary>行情数据源抽象。实现类负责从具体接口（东财/新浪等）取回单只股票行情。</summary>
public interface IQuoteSource
{
    /// <summary>
    /// 获取单只股票的行情快照。数据源无该股票或返回异常数据时返回 <see langword="null"/>。
    /// </summary>
    /// <exception cref="HttpRequestException">网络请求在重试后仍失败。</exception>
    Task<Quote?> GetQuoteAsync(StockCode code, CancellationToken cancellationToken = default);
}
