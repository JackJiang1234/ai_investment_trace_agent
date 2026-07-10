namespace Agent.Core;

/// <summary>交易日判断。纯函数。</summary>
/// <remarks>
/// 本期规则：排除周六、周日，以及配置的节假日列表。
/// 未内置中国法定节假日日历——需要精确时由配置的 <c>Holidays</c> 提供。
/// </remarks>
public static class TradingCalendar
{
    public static bool IsTradingDay(DateOnly date, IReadOnlyList<DateOnly> holidays)
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return false;
        }

        return !holidays.Contains(date);
    }
}
