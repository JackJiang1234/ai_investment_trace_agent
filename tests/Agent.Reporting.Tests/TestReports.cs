using Agent.Core;

namespace Agent.Reporting.Tests;

/// <summary>测试用的报告数据构造器。</summary>
internal static class TestReports
{
    public static Quote Quote(string code, string name, decimal price) => new()
    {
        Code = StockCode.Parse(code),
        Name = name,
        Price = price,
        PreviousClose = price,
        Open = price,
        High = price,
        Low = price,
        ChangeAmount = 0m,
        ChangePercent = 0m,
        Volume = 12345,
        Turnover = 1_000_000m,
        TurnoverRate = 1.2m,
        VolumeRatio = 1m,
        TotalMarketCap = 0m,
    };

    public static DailyReport Sample()
    {
        // 核心持仓：命中击球（15000 亿 < 18000 亿）。
        var maotai = new StockAnalysis
        {
            Quote = Quote("600519.SH", "贵州茅台", 1199.30m),
            Holding = new HoldingMetrics
            {
                Group = "核心持仓",
                Currency = Currency.CNY,
                Shares = 100,
                CostPrice = 1450m,
                TotalMarketCapCnyYi = 15000m,
                HoldingValueCny = 119930m,
                YearStartValueCny = 126000m,
                IdealBuyYi = 18000m,
                SellYi = 28000m,
                YtdReturnPercent = -5.2m,
                CanBuy = true,
                HoldingRatioPercent = 100m,
            },
        };

        // 观察池：无持仓，仅估值与买点。
        var tencent = new StockAnalysis
        {
            Quote = Quote("00700.HK", "腾讯控股", 476.40m),
            Holding = new HoldingMetrics
            {
                Group = "观察池",
                Currency = Currency.HKD,
                TotalMarketCapCnyYi = 36000m,
                IdealBuyYi = 30000m,
                YtdReturnPercent = 8.1m,
                CanBuy = false,
            },
        };

        IReadOnlyList<StockAnalysis> stocks = [maotai, tencent];
        return new DailyReport
        {
            Date = new DateOnly(2026, 7, 10),
            Summary = PortfolioSummary.From(stocks),
            Stocks = stocks,
            HkdToCny = 0.87m,
            IsLiveRate = true,
        };
    }
}
