using System.Globalization;
using System.Text;
using System.Text.Json;
using Agent.Core;

namespace Agent.DataSource;

/// <summary>
/// 解析东方财富公告接口（<c>np-anotice-stock.eastmoney.com/api/security/ann</c>）返回的 JSON。纯函数。
/// </summary>
/// <remarks>
/// 结构：<c>data.list[]</c>，每项含 <c>art_code</c>、<c>codes[].stock_code</c>、
/// <c>columns[].column_name</c>（类型，可多个）、<c>notice_date</c>、<c>title_ch</c>。
/// 缺少关键字段（标题/日期/art_code/stock_code）的项会被跳过。
/// </remarks>
public static class EastMoneyAnnouncementParser
{
    public static IReadOnlyList<Announcement> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("data", out var data) ||
                data.ValueKind != JsonValueKind.Object ||
                !data.TryGetProperty("list", out var list) ||
                list.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var result = new List<Announcement>(list.GetArrayLength());
            foreach (var item in list.EnumerateArray())
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

        var title = GetString(item, "title_ch");
        if (string.IsNullOrEmpty(title))
        {
            title = GetString(item, "title");
        }

        var artCode = GetString(item, "art_code");
        var stockCode = FirstStockCode(item);

        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(artCode) || stockCode is null)
        {
            return false;
        }

        if (!TryParseDate(GetString(item, "notice_date"), out var date))
        {
            return false;
        }

        ann = new Announcement
        {
            Date = date,
            Title = title,
            Type = JoinColumnNames(item),
            Url = $"https://data.eastmoney.com/notices/detail/{stockCode}/{artCode}.html",
        };
        return true;
    }

    private static bool TryParseDate(string raw, out DateOnly date)
    {
        date = default;
        if (raw.Length < 10)
        {
            return false;
        }

        return DateOnly.TryParse(raw.AsSpan(0, 10), CultureInfo.InvariantCulture, out date);
    }

    private static string? FirstStockCode(JsonElement item)
    {
        if (item.TryGetProperty("codes", out var codes) &&
            codes.ValueKind == JsonValueKind.Array)
        {
            foreach (var c in codes.EnumerateArray())
            {
                var code = GetString(c, "stock_code");
                if (!string.IsNullOrEmpty(code))
                {
                    return code;
                }
            }
        }

        return null;
    }

    private static string JoinColumnNames(JsonElement item)
    {
        if (!item.TryGetProperty("columns", out var columns) ||
            columns.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        foreach (var col in columns.EnumerateArray())
        {
            var name = GetString(col, "column_name");
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            if (sb.Length > 0)
            {
                sb.Append('/');
            }

            sb.Append(name);
        }

        return sb.ToString();
    }

    private static string GetString(JsonElement obj, string field) =>
        obj.TryGetProperty(field, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString() ?? string.Empty
            : string.Empty;
}
