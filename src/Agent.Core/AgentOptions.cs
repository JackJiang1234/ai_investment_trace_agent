namespace Agent.Core;

/// <summary>单只股票的配置项。</summary>
public sealed class StockConfig
{
    /// <summary>股票代码，形如 600519.SH / 00700.HK。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>分组标签，如"核心持仓"/"观察池"。</summary>
    public string? Group { get; set; }
}

/// <summary>报告输出配置。</summary>
public sealed class ReportOptions
{
    /// <summary>报告输出目录（相对或绝对路径）。</summary>
    public string OutputDirectory { get; set; } = "reports";

    /// <summary>输出格式列表。</summary>
    public List<ReportFormat> Formats { get; set; } = [ReportFormat.Html, ReportFormat.Markdown];

    /// <summary>拉取历史K线的根数（供 MA60 等指标使用，留足缓冲）。</summary>
    public int KlineLookbackDays { get; set; } = 70;
}

/// <summary>根配置，绑定 appsettings.json。</summary>
public sealed class AgentOptions
{
    public const string SectionName = "Agent";

    /// <summary>关注的股票列表。</summary>
    public List<StockConfig> Stocks { get; set; } = [];

    /// <summary>异动阈值。</summary>
    public AlertOptions Alerts { get; set; } = new();

    /// <summary>报告输出配置。</summary>
    public ReportOptions Report { get; set; } = new();
}
