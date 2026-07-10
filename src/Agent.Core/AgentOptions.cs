namespace Agent.Core;

/// <summary>单只股票的配置项。</summary>
public sealed class StockConfig
{
    /// <summary>股票代码，形如 600519.SH / 00700.HK。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>分组标签，如"核心持仓"/"观察池"。</summary>
    public string? Group { get; set; }
}

/// <summary>公告追踪配置。</summary>
public sealed class AnnouncementOptions
{
    /// <summary>是否仅保留重要类型公告（按 <see cref="IncludeTypes"/> 关键词过滤类型/标题）。</summary>
    public bool ImportantTypesOnly { get; set; } = true;

    /// <summary>重要类型关键词（命中公告类型或标题即保留）。</summary>
    public List<string> IncludeTypes { get; set; } =
    [
        "业绩", "年报", "季报", "快报", "预告", "分配", "分红", "分派", "重组", "收购",
        "增持", "减持", "质押", "冻结", "诉讼", "仲裁", "处罚", "违规", "问询", "停牌", "复牌", "回购",
    ];

    /// <summary>每只股票拉取的公告条数。</summary>
    public int Lookback { get; set; } = 20;

    /// <summary>港股例行公告排除关键词（英文，命中类型/标题即剔除，如每日披露/月报）。</summary>
    public List<string> HkExcludeTypes { get; set; } =
    [
        "Next Day Disclosure", "Monthly Return", "Proxy Form", "Notification Letter",
    ];
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

    /// <summary>公告追踪配置。</summary>
    public AnnouncementOptions Announcements { get; set; } = new();

    /// <summary>风险关键词（命中公告标题即在风险提示区高亮）。</summary>
    public List<string> RiskKeywords { get; set; } =
    [
        "减持", "质押", "冻结", "诉讼", "仲裁", "处罚", "违规", "ST", "退市",
        "问询", "商誉", "停牌", "下调评级", "立案", "风险警示",
    ];

    /// <summary>报告输出配置。</summary>
    public ReportOptions Report { get; set; } = new();
}
