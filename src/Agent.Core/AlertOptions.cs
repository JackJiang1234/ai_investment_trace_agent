namespace Agent.Core;

/// <summary>异动提醒阈值配置。阈值 ≤ 0 表示禁用该项规则。</summary>
public sealed record AlertOptions
{
    /// <summary>涨跌幅阈值（百分比绝对值）。|涨跌幅| ≥ 阈值 即命中。</summary>
    public decimal PctChangeThreshold { get; init; } = 5m;

    /// <summary>量比阈值。量比 ≥ 阈值 即命中。</summary>
    public decimal VolumeRatioThreshold { get; init; } = 2m;
}
