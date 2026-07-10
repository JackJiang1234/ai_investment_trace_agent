using System.Globalization;
using System.Net;
using System.Text.Json;
using Agent.Core;

namespace Agent.DataSource;

/// <summary>
/// 解析香港交易所披露易（hkexnews）接口响应。纯函数。
/// </summary>
/// <remarks>
/// - <c>prefix.do</c>：JSONP 包裹 <c>callback({...})</c>，内含 <c>stockInfo[].stockId</c>。
/// - <c>titleSearchServlet.do</c>：外层 JSON 的 <c>result</c> 字段是二次编码的 JSON 数组字符串，
///   每项含 <c>TITLE</c>、<c>LONG_TEXT</c>（类型）、<c>DATE_TIME</c>（dd/MM/yyyy HH:mm）、<c>FILE_LINK</c>。
/// </remarks>
public static class HkexnewsAnnouncementParser
{
    private const string BaseUrl = "https://www1.hkexnews.hk";

    /// <summary>从 prefix.do 的 JSONP 响应解析内部 stockId；失败返回 null。</summary>
    public static int? ParseStockId(string jsonp)
    {
        if (string.IsNullOrWhiteSpace(jsonp))
        {
            return null;
        }

        var start = jsonp.IndexOf('(');
        var end = jsonp.LastIndexOf(')');
        if (start < 0 || end <= start)
        {
            return null;
        }

        var json = jsonp[(start + 1)..end];
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("stockInfo", out var info) &&
                info.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in info.EnumerateArray())
                {
                    if (s.TryGetProperty("stockId", out var id) &&
                        id.ValueKind == JsonValueKind.Number)
                    {
                        return id.GetInt32();
                    }
                }
            }
        }
        catch (JsonException)
        {
            // fall through
        }

        return null;
    }

    /// <summary>解析 titleSearchServlet 响应为公告列表；失败返回空。</summary>
    public static IReadOnlyList<Announcement> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using var outer = JsonDocument.Parse(json);
            if (!outer.RootElement.TryGetProperty("result", out var resultEl) ||
                resultEl.ValueKind != JsonValueKind.String)
            {
                return [];
            }

            var inner = resultEl.GetString();
            if (string.IsNullOrWhiteSpace(inner))
            {
                return [];
            }

            using var arr = JsonDocument.Parse(inner);
            if (arr.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var result = new List<Announcement>(arr.RootElement.GetArrayLength());
            foreach (var item in arr.RootElement.EnumerateArray())
            {
                if (TryParseItem(item, out var ann))
                {
                    result.Add(ann);
                }
            }

            return result;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static bool TryParseItem(JsonElement item, out Announcement ann)
    {
        ann = null!;

        var title = Clean(GetString(item, "TITLE"));
        var link = GetString(item, "FILE_LINK");
        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(link))
        {
            return false;
        }

        if (!TryParseDate(GetString(item, "DATE_TIME"), out var date))
        {
            return false;
        }

        var type = Clean(GetString(item, "LONG_TEXT"));

        ann = new Announcement
        {
            Date = date,
            Title = title,
            Type = type,
            Url = link.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? link : BaseUrl + link,
        };
        return true;
    }

    private static bool TryParseDate(string raw, out DateOnly date)
    {
        date = default;
        // 形如 "09/07/2026 17:58"，取日期部分 dd/MM/yyyy。
        if (raw.Length < 10)
        {
            return false;
        }

        return DateOnly.TryParseExact(
            raw.AsSpan(0, 10), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    /// <summary>去除 HTML 实体与 &lt;br/&gt; 标签。</summary>
    private static string Clean(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var decoded = WebUtility.HtmlDecode(text);
        return decoded.Replace("<br/>", " ").Replace("<br>", " ").Trim();
    }

    private static string GetString(JsonElement obj, string field) =>
        obj.TryGetProperty(field, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString() ?? string.Empty
            : string.Empty;
}
