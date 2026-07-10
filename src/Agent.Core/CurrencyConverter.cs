namespace Agent.Core;

/// <summary>
/// 货币折算器：将各币种金额折算为人民币（基准货币）。
/// 汇率在构造时确定（实时源或回退静态值），折算本身为纯计算、可测。
/// </summary>
public sealed class CurrencyConverter
{
    private readonly decimal _hkdToCny;

    /// <param name="hkdToCny">1 港元折人民币的汇率（须为正）。</param>
    /// <param name="isLiveRate">是否取自实时源（false 表示回退静态值），供报告脚注标注。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="hkdToCny"/> 非正。</exception>
    public CurrencyConverter(decimal hkdToCny, bool isLiveRate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hkdToCny);
        _hkdToCny = hkdToCny;
        IsLiveRate = isLiveRate;
    }

    /// <summary>所用 HKD→CNY 汇率。</summary>
    public decimal HkdToCny => _hkdToCny;

    /// <summary>汇率是否来自实时源（false 表示回退静态值）。</summary>
    public bool IsLiveRate { get; }

    /// <summary>将 <paramref name="amount"/>（以 <paramref name="currency"/> 计价）折算为人民币。</summary>
    public decimal ToCny(decimal amount, Currency currency) => currency switch
    {
        Currency.HKD => amount * _hkdToCny,
        _ => amount,
    };
}
