namespace Agent.Core;

/// <summary>
/// 将行情快照与历史K线组合为 <see cref="StockAnalysis"/>：计算技术指标并判定异动。纯函数。
/// </summary>
public static class StockAnalyzer
{
    private const int NewRangePeriod = 20;

    public static StockAnalysis Analyze(
        Quote quote, IReadOnlyList<DailyBar> bars, AlertOptions alertOptions)
    {
        var closes = new decimal[bars.Count];
        for (var i = 0; i < bars.Count; i++)
        {
            closes[i] = bars[i].Close;
        }

        return new StockAnalysis
        {
            Quote = quote,
            Ma5 = Indicators.MovingAverage(closes, 5),
            Ma20 = Indicators.MovingAverage(closes, 20),
            Ma60 = Indicators.MovingAverage(closes, 60),
            Return5 = Indicators.PeriodReturnPercent(closes, 5),
            Return20 = Indicators.PeriodReturnPercent(closes, 20),
            IsNewHigh20 = Indicators.IsPeriodHigh(bars, NewRangePeriod),
            IsNewLow20 = Indicators.IsPeriodLow(bars, NewRangePeriod),
            Alerts = AlertEvaluator.Evaluate(quote, alertOptions),
        };
    }
}
