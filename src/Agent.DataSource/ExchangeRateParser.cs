using System.Text.Json;

namespace Agent.DataSource;

/// <summary>
/// 解析东财 push2 外汇接口（<c>/api/qt/stock/get</c>）返回的汇率 JSON。纯函数、无网络依赖。
/// 汇率 = f43 / 10^f59（与价格字段同规则）。
/// </summary>
public static class ExchangeRateParser
{
    /// <summary>解析汇率；<c>data</c> 为空、非法 JSON 或非正值时返回 <see langword="null"/>。</summary>
    public static decimal? Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("data", out var data) ||
                data.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return null;
            }

            if (!data.TryGetProperty("f43", out var f43) || f43.ValueKind != JsonValueKind.Number)
            {
                return null;
            }

            var scale = data.TryGetProperty("f59", out var f59) && f59.ValueKind == JsonValueKind.Number
                ? (decimal)Math.Pow(10, f59.GetInt32())
                : 1m;

            var rate = f43.GetDecimal() / scale;
            return rate <= 0 ? null : rate;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
