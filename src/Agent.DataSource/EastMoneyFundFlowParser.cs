using System.Globalization;
using System.Text.Json;
using Agent.Core;

namespace Agent.DataSource;

/// <summary>
/// 解析东方财富 push2his 资金流向接口（<c>/api/qt/stock/fflow/kline/get</c>）返回的 JSON。纯函数。
/// </summary>
/// <remarks>
/// <c>data.klines</c> 每行逗号分隔，列序：
/// <c>日期,主力净流入,小单净流入,中单净流入,大单净流入,超大单净流入</c>（单位：元）。
/// 字段不足或无法解析的行会被跳过。
/// </remarks>
public static class EastMoneyFundFlowParser
{
    private const int ExpectedColumns = 6;
    private static readonly char[] Separator = [','];

    public static IReadOnlyList<FundFlow> Parse(string json)
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

            var flows = new List<FundFlow>(klines.GetArrayLength());
            foreach (var line in klines.EnumerateArray())
            {
                if (line.ValueKind == JsonValueKind.String &&
                    TryParseLine(line.GetString(), out var flow))
                {
                    flows.Add(flow);
                }
            }

            return flows;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static bool TryParseLine(string? line, out FundFlow flow)
    {
        flow = null!;
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
            !TryDecimal(f[1], out var main) ||
            !TryDecimal(f[2], out var small) ||
            !TryDecimal(f[3], out var medium) ||
            !TryDecimal(f[4], out var large) ||
            !TryDecimal(f[5], out var superLarge))
        {
            return false;
        }

        flow = new FundFlow
        {
            Date = date,
            MainNetInflow = main,
            SmallNetInflow = small,
            MediumNetInflow = medium,
            LargeNetInflow = large,
            SuperLargeNetInflow = superLarge,
        };
        return true;
    }

    private static bool TryDecimal(string s, out decimal value) =>
        decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
}
