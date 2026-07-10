namespace Agent.Core;

/// <summary>报告输出格式。</summary>
public enum ReportFormat
{
    /// <summary>Markdown。</summary>
    Markdown,

    /// <summary>HTML（带 &lt;style&gt; 的响应式，浏览器/GitHub Pages 查看）。</summary>
    Html,

    /// <summary>公众号导出：全内联样式、无 &lt;style&gt;/&lt;script&gt;/&lt;head&gt;，可粘贴进微信公众号编辑器。</summary>
    WeChatHtml,
}

/// <summary>已渲染的报告：各格式的文本内容。</summary>
public sealed record RenderedReport
{
    /// <summary>报告日期。</summary>
    public required DateOnly Date { get; init; }

    /// <summary>格式 → 渲染后的文本内容。</summary>
    public required IReadOnlyDictionary<ReportFormat, string> Contents { get; init; }
}

/// <summary>将 <see cref="DailyReport"/> 渲染为指定格式的文本。</summary>
public interface IReportRenderer
{
    string Render(DailyReport report, ReportFormat format);
}

/// <summary>投递已渲染的报告（默认写文件；企业微信/Server酱为预留实现）。</summary>
public interface IReportDelivery
{
    Task DeliverAsync(RenderedReport report, CancellationToken cancellationToken = default);
}

/// <summary>报告摘要器。默认 NoOp（关闭）；启用后由 LLM 生成摘要。</summary>
public interface ISummarizer
{
    /// <summary>返回摘要文本；不生成时返回 <see langword="null"/>。</summary>
    Task<string?> SummarizeAsync(DailyReport report, CancellationToken cancellationToken = default);
}
