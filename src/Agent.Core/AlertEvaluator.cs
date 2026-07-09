namespace Agent.Core;

/// <summary>依阈值配置判定单只股票当日是否触发异动提醒。纯函数。</summary>
public static class AlertEvaluator
{
    public static IReadOnlyList<TriggeredAlert> Evaluate(Quote quote, AlertOptions options)
    {
        var alerts = new List<TriggeredAlert>(2);

        if (options.PctChangeThreshold > 0 &&
            Math.Abs(quote.ChangePercent) >= options.PctChangeThreshold)
        {
            var direction = quote.ChangePercent >= 0 ? "涨幅" : "跌幅";
            alerts.Add(new TriggeredAlert
            {
                Kind = AlertKind.PriceChange,
                Description = $"{direction} {Math.Abs(quote.ChangePercent):0.##}% ≥ {options.PctChangeThreshold:0.##}%",
                Value = quote.ChangePercent,
                Threshold = options.PctChangeThreshold,
            });
        }

        if (options.VolumeRatioThreshold > 0 &&
            quote.VolumeRatio >= options.VolumeRatioThreshold)
        {
            alerts.Add(new TriggeredAlert
            {
                Kind = AlertKind.VolumeRatio,
                Description = $"量比 {quote.VolumeRatio:0.##} ≥ {options.VolumeRatioThreshold:0.##}",
                Value = quote.VolumeRatio,
                Threshold = options.VolumeRatioThreshold,
            });
        }

        return alerts;
    }
}
