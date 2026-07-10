using System.Text.Json;
using Agent.Core;

namespace Agent.DataSource;

/// <summary>
/// 解析东方财富 push2 行情接口（<c>/api/qt/stock/get</c>）返回的 JSON。
/// 纯函数、无网络依赖，便于单元测试。
/// </summary>
/// <remarks>
/// 数值缩放规则（依真实响应实测）：
/// <list type="bullet">
/// <item>价格类（f43/f44/f45/f46/f60）与涨跌额（f169）：除以 10^f59（f59 为价格小数位，A股=2、港股=3）。</item>
/// <item>涨跌幅 f170、换手率 f168、量比 f50：固定除以 100。</item>
/// <item>成交量 f47、成交额 f48：原始值，不缩放。</item>
/// </list>
/// </remarks>
public static class EastMoneyQuoteParser
{
    // 百分比/比率字段的固定缩放系数（两位小数）。
    private const decimal PercentScale = 100m;

    /// <summary>
    /// 解析行情 JSON。当 <c>data</c> 为空（无效代码/停牌）或 JSON 非法时返回 <see langword="null"/>。
    /// </summary>
    public static Quote? Parse(string json, StockCode code)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        JsonElement data;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("data", out data) ||
                data.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return null;
            }

            // data 是独立 JsonDocument 的一部分，doc 释放后不可用 → 立即取值。
            var priceScale = (decimal)Math.Pow(10, GetInt(data, "f59"));

            return new Quote
            {
                Code = code,
                Name = GetString(data, "f58"),
                Price = GetDecimal(data, "f43") / priceScale,
                PreviousClose = GetDecimal(data, "f60") / priceScale,
                Open = GetDecimal(data, "f46") / priceScale,
                High = GetDecimal(data, "f44") / priceScale,
                Low = GetDecimal(data, "f45") / priceScale,
                ChangeAmount = GetDecimal(data, "f169") / priceScale,
                ChangePercent = GetDecimal(data, "f170") / PercentScale,
                Volume = (long)GetDecimal(data, "f47"),
                Turnover = GetDecimal(data, "f48"),
                TurnoverRate = GetDecimal(data, "f168") / PercentScale,
                VolumeRatio = GetDecimal(data, "f50") / PercentScale,
                // f116 总市值为实际货币金额，不缩放（与价格字段不同）。
                TotalMarketCap = GetDecimal(data, "f116"),
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static decimal GetDecimal(JsonElement obj, string field) =>
        obj.TryGetProperty(field, out var el) && el.ValueKind == JsonValueKind.Number
            ? el.GetDecimal()
            : 0m;

    private static int GetInt(JsonElement obj, string field) =>
        obj.TryGetProperty(field, out var el) && el.ValueKind == JsonValueKind.Number
            ? el.GetInt32()
            : 0;

    private static string GetString(JsonElement obj, string field) =>
        obj.TryGetProperty(field, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString() ?? string.Empty
            : string.Empty;
}
