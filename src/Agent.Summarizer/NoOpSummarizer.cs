using Agent.Core;

namespace Agent.Summarizer;

/// <summary>
/// 默认摘要器：不做任何事，返回 null（不生成摘要）。
/// 本期所有功能为纯规则，零大模型成本；启用 AI 摘要需替换为 LLM 实现。
/// </summary>
public sealed class NoOpSummarizer : ISummarizer
{
    public Task<string?> SummarizeAsync(DailyReport report, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(null);
}
