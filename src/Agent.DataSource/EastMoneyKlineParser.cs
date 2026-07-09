using System.Globalization;
using System.Text.Json;
using Agent.Core;

namespace Agent.DataSource;

/// <summary>
/// 解析东方财富 push2his 日K线接口（<c>/api/qt/stock/kline/get</c>）返回的 JSON。
/// 纯函数、无网络依赖。
/// </summary>
/// <remarks>
/// <c>data.klines</c> 为逗号分隔字符串数组，列序（依 fields2 请求顺序 f51..f61）：
/// <c>日期,开,收,高,低,成交量,成交额,振幅,涨跌幅,涨跌额,换手率</c>。
/// 价格已是可读小数，无需缩放。字段不足或无法解析的行会被跳过而非抛出。
/// </remarks>
public static class EastMoneyKlineParser
{
    private const int ExpectedColumns = 11;
    private static readonly char[] Separator = [','];

    public static IReadOnlyList<DailyBar> Parse(string json)
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
                !data.TryGetProperty("klines", out var klines) ||
                klines.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var bars = new List<DailyBar>(klines.GetArrayLength());
            foreach (var line in klines.EnumerateArray())
            {
                if (line.ValueKind == JsonValueKind.String &&
                    TryParseLine(line.GetString(), out var bar))
                {
                    bars.Add(bar);
                }
            }

            return bars;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static bool TryParseLine(string? line, out DailyBar bar)
    {
        bar = null!;
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var f = line.Split(Separator);
        if (f.Length < ExpectedColumns)
        {
            return false;
        }

        if (!DateOnly.TryParse(f[0], CultureInfo.InvariantCulture, out var date) ||
            !TryDecimal(f[1], out var open) ||
            !TryDecimal(f[2], out var close) ||
            !TryDecimal(f[3], out var high) ||
            !TryDecimal(f[4], out var low) ||
            !long.TryParse(f[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var volume) ||
            !TryDecimal(f[6], out var amount) ||
            !TryDecimal(f[8], out var changePercent) ||
            !TryDecimal(f[10], out var turnoverRate))
        {
            return false;
        }

        bar = new DailyBar
        {
            Date = date,
            Open = open,
            Close = close,
            High = high,
            Low = low,
            Volume = volume,
            Amount = amount,
            ChangePercent = changePercent,
            TurnoverRate = turnoverRate,
        };
        return true;
    }

    private static bool TryDecimal(string s, out decimal value) =>
        decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
}
