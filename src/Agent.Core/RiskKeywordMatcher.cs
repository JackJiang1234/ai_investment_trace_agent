namespace Agent.Core;

/// <summary>在公告标题中匹配风险关键词。纯函数、不解读内容。</summary>
public static class RiskKeywordMatcher
{
    /// <summary>
    /// 返回标题命中的第一个关键词（按 <paramref name="keywords"/> 顺序）；无命中返回 <see langword="null"/>。
    /// </summary>
    public static string? FindMatch(string title, IReadOnlyList<string> keywords)
    {
        if (string.IsNullOrEmpty(title))
        {
            return null;
        }

        foreach (var kw in keywords)
        {
            if (!string.IsNullOrEmpty(kw) && title.Contains(kw, StringComparison.Ordinal))
            {
                return kw;
            }
        }

        return null;
    }
}
