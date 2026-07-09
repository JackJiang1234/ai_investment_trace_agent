namespace Agent.Core;

/// <summary>
/// 技术指标纯函数。所有方法不改变入参、无副作用，便于穷举边界测试。
/// 收盘价序列约定为按日期升序（最早在前，最新在末）。
/// </summary>
public static class Indicators
{
    /// <summary>
    /// 最近 <paramref name="period"/> 个收盘价的简单移动平均。数据不足返回 <see langword="null"/>。
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="period"/> 非正。</exception>
    public static decimal? MovingAverage(IReadOnlyList<decimal> closes, int period)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(period);

        if (closes.Count < period)
        {
            return null;
        }

        decimal sum = 0;
        for (var i = closes.Count - period; i < closes.Count; i++)
        {
            sum += closes[i];
        }

        return sum / period;
    }

    /// <summary>
    /// 近 <paramref name="period"/> 日涨跌幅（百分比）：最新收盘价相对 <paramref name="period"/> 个交易日前收盘价。
    /// 数据不足（少于 period+1 个点）或基准价为 0 时返回 <see langword="null"/>。
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="period"/> 非正。</exception>
    public static decimal? PeriodReturnPercent(IReadOnlyList<decimal> closes, int period)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(period);

        if (closes.Count < period + 1)
        {
            return null;
        }

        var latest = closes[^1];
        var basePrice = closes[^(period + 1)];
        if (basePrice == 0)
        {
            return null;
        }

        return (latest - basePrice) / basePrice * 100m;
    }

    /// <summary>
    /// 最新一根K线是否创近 <paramref name="period"/> 日新高（其最高价为窗口内最高）。
    /// 数据不足 <paramref name="period"/> 根时返回 <see langword="false"/>。
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="period"/> 非正。</exception>
    public static bool IsPeriodHigh(IReadOnlyList<DailyBar> bars, int period)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(period);

        if (bars.Count < period)
        {
            return false;
        }

        var latestHigh = bars[^1].High;
        for (var i = bars.Count - period; i < bars.Count; i++)
        {
            if (bars[i].High > latestHigh)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 最新一根K线是否创近 <paramref name="period"/> 日新低（其最低价为窗口内最低）。
    /// 数据不足 <paramref name="period"/> 根时返回 <see langword="false"/>。
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="period"/> 非正。</exception>
    public static bool IsPeriodLow(IReadOnlyList<DailyBar> bars, int period)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(period);

        if (bars.Count < period)
        {
            return false;
        }

        var latestLow = bars[^1].Low;
        for (var i = bars.Count - period; i < bars.Count; i++)
        {
            if (bars[i].Low < latestLow)
            {
                return false;
            }
        }

        return true;
    }
}
