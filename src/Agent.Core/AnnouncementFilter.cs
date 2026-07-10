namespace Agent.Core;

/// <summary>按配置的重要类型关键词过滤公告。纯函数。</summary>
public static class AnnouncementFilter
{
    /// <summary>
    /// 当 <see cref="AnnouncementOptions.ImportantTypesOnly"/> 为 true 时，
    /// 仅保留类型或标题命中任一 <see cref="AnnouncementOptions.IncludeTypes"/> 关键词的公告；
    /// 否则原样返回。
    /// </summary>
    public static IReadOnlyList<Announcement> FilterImportant(
        IReadOnlyList<Announcement> announcements, AnnouncementOptions options)
    {
        if (!options.ImportantTypesOnly)
        {
            return announcements;
        }

        var keywords = options.IncludeTypes;
        var result = new List<Announcement>(announcements.Count);
        foreach (var ann in announcements)
        {
            if (ContainsAny(ann.Type, keywords) || ContainsAny(ann.Title, keywords))
            {
                result.Add(ann);
            }
        }

        return result;
    }

    /// <summary>
    /// 剔除类型或标题命中任一 <paramref name="excludeKeywords"/> 的公告（用于港股排除例行件）。
    /// 排除列表为空时原样返回。
    /// </summary>
    public static IReadOnlyList<Announcement> ExcludeRoutine(
        IReadOnlyList<Announcement> announcements, IReadOnlyList<string> excludeKeywords)
    {
        if (excludeKeywords.Count == 0)
        {
            return announcements;
        }

        var result = new List<Announcement>(announcements.Count);
        foreach (var ann in announcements)
        {
            if (!ContainsAny(ann.Type, excludeKeywords) && !ContainsAny(ann.Title, excludeKeywords))
            {
                result.Add(ann);
            }
        }

        return result;
    }

    private static bool ContainsAny(string text, IReadOnlyList<string> keywords)
    {
        foreach (var kw in keywords)
        {
            if (!string.IsNullOrEmpty(kw) && text.Contains(kw, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
