using Agent.Core;

namespace Agent.Reporting.Tests;

/// <summary>测试用的报告数据构造器。</summary>
internal static class TestReports
{
    public static Quote Quote(string code, string name, decimal price, decimal changePercent,
        decimal volumeRatio = 1m) => new()
    {
        Code = StockCode.Parse(code),
        Name = name,
        Price = price,
        PreviousClose = price,
        Open = price,
        High = price,
        Low = price,
        ChangeAmount = 0m,
        ChangePercent = changePercent,
        Volume = 12345,
        Turnover = 1_000_000m,
        TurnoverRate = 1.2m,
        VolumeRatio = volumeRatio,
    };

    public static StockAnalysis Analysis(Quote quote, decimal? ma5 = null,
        params TriggeredAlert[] alerts) => new()
    {
        Quote = quote,
        Ma5 = ma5,
        Ma20 = null,
        Ma60 = null,
        Return5 = null,
        Return20 = null,
        IsNewHigh20 = false,
        IsNewLow20 = false,
        Alerts = alerts,
    };

    public static DailyReport Sample()
    {
        var maotai = Analysis(Quote("600519.SH", "贵州茅台", 1199.30m, 0.88m), ma5: 1195.00m);
        var tencent = Analysis(
            Quote("00700.HK", "腾讯控股", 476.40m, 6.30m, volumeRatio: 2.5m),
            ma5: 460.00m,
            new TriggeredAlert
            {
                Kind = AlertKind.PriceChange,
                Description = "涨幅 6.3% ≥ 5%",
                Value = 6.30m,
                Threshold = 5m,
            });

        IReadOnlyList<StockAnalysis> stocks = [maotai, tencent];
        return new DailyReport
        {
            Date = new DateOnly(2026, 7, 8),
            Summary = PortfolioSummary.From(stocks),
            Stocks = stocks,
        };
    }
}
