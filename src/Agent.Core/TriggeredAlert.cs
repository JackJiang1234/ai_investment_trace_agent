namespace Agent.Core;

/// <summary>异动类型。</summary>
public enum AlertKind
{
    /// <summary>涨跌幅超阈值。</summary>
    PriceChange,

    /// <summary>量比超阈值。</summary>
    VolumeRatio,
}

/// <summary>一条命中的异动提醒。</summary>
public sealed record TriggeredAlert
{
    /// <summary>异动类型。</summary>
    public required AlertKind Kind { get; init; }

    /// <summary>面向报告的中文描述。</summary>
    public required string Description { get; init; }

    /// <summary>触发时的实际值。</summary>
    public required decimal Value { get; init; }

    /// <summary>命中的阈值。</summary>
    public required decimal Threshold { get; init; }
}
