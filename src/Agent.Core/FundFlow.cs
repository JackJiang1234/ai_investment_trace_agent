namespace Agent.Core;

/// <summary>
/// 单个交易日的资金流向（单位：元）。主力 = 大单 + 超大单。
/// </summary>
public sealed record FundFlow
{
    /// <summary>交易日期。</summary>
    public required DateOnly Date { get; init; }

    /// <summary>主力净流入（= 大单 + 超大单）。</summary>
    public required decimal MainNetInflow { get; init; }

    /// <summary>超大单净流入。</summary>
    public required decimal SuperLargeNetInflow { get; init; }

    /// <summary>大单净流入。</summary>
    public required decimal LargeNetInflow { get; init; }

    /// <summary>中单净流入。</summary>
    public required decimal MediumNetInflow { get; init; }

    /// <summary>小单净流入。</summary>
    public required decimal SmallNetInflow { get; init; }

    /// <summary>主力净流入（亿元），供展示用。</summary>
    public decimal MainNetInflowInYi => MainNetInflow / 100_000_000m;
}
