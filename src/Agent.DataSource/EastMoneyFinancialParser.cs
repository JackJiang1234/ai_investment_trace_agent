using System.Globalization;
using System.Text.Json;
using Agent.Core;

namespace Agent.DataSource;

/// <summary>
/// 解析东方财富 datacenter 财报接口（<c>datacenter-web.eastmoney.com/api/data/v1/get</c>）响应。纯函数。
/// </summary>
/// <remarks>
/// 结构：<c>result.data[]</c>；无数据时 <c>result</c> 为 null（success=false）。取首行（最新）。
/// </remarks>
public static class EastMoneyFinancialParser
{
    /// <summary>解析业绩报表（RPT_LICO_FN_CPD）首行为关键财务数据。</summary>
    public static FinancialSnapshot? ParseSnapshot(string json)
    {
        var row = FirstDataRow(json);
        if (row is not { } r)
        {
            return null;
        }

        if (!TryDate(GetString(r, "REPORTDATE"), out var period))
        {
            return null;
        }

        TryDate(GetString(r, "NOTICE_DATE"), out var notice);

        return new FinancialSnapshot
        {
            ReportPeriod = period,
            PeriodLabel = GetString(r, "DATATYPE"),
            NoticeDate = notice,
            Revenue = GetDecimal(r, "TOTAL_OPERATE_INCOME") ?? 0m,
            NetProfit = GetDecimal(r, "PARENT_NETPROFIT") ?? 0m,
            RevenueYoY = GetDecimal(r, "YSTZ"),
            NetProfitYoY = GetDecimal(r, "SJLTZ"),
            Roe = GetDecimal(r, "WEIGHTAVG_ROE"),
            GrossMargin = GetDecimal(r, "XSMLL"),
            Eps = GetDecimal(r, "BASIC_EPS"),
        };
    }

    /// <summary>解析业绩预告（RPT_PUBLIC_OP_NEWPREDICT）首行。</summary>
    public static EarningsForecast? ParseForecast(string json)
    {
        var row = FirstDataRow(json);
        if (row is not { } r)
        {
            return null;
        }

        if (!TryDate(GetString(r, "NOTICE_DATE"), out var notice) ||
            !TryDate(GetString(r, "REPORT_DATE"), out var period))
        {
            return null;
        }

        return new EarningsForecast
        {
            NoticeDate = notice,
            ReportPeriod = period,
            Type = GetString(r, "PREDICT_TYPE"),
            Content = GetString(r, "PREDICT_CONTENT"),
        };
    }

    private static JsonElement? FirstDataRow(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("result", out var result) ||
                result.ValueKind != JsonValueKind.Object ||
                !result.TryGetProperty("data", out var data) ||
                data.ValueKind != JsonValueKind.Array ||
                data.GetArrayLength() == 0)
            {
                return null;
            }

            // Clone so the element stays valid after the document is disposed.
            return data[0].Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryDate(string raw, out DateOnly date)
    {
        date = default;
        return raw.Length >= 10 &&
               DateOnly.TryParse(raw.AsSpan(0, 10), CultureInfo.InvariantCulture, out date);
    }

    private static decimal? GetDecimal(JsonElement obj, string field) =>
        obj.TryGetProperty(field, out var el) && el.ValueKind == JsonValueKind.Number
            ? el.GetDecimal()
            : null;

    private static string GetString(JsonElement obj, string field) =>
        obj.TryGetProperty(field, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString() ?? string.Empty
            : string.Empty;
}
