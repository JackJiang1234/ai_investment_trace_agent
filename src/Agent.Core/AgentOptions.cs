namespace Agent.Core;

/// <summary>单只股票的配置项。持仓/买卖点字段均可空（观察池股票通常不填持仓）。</summary>
public sealed class StockConfig
{
    /// <summary>股票代码，形如 600519.SH / 00700.HK。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>分组标签，如"核心持仓"/"观察池"。</summary>
    public string? Group { get; set; }

    /// <summary>持股份数；观察池可空。</summary>
    public long? Shares { get; set; }

    /// <summary>成本价（每股，原币种）；观察池可空。</summary>
    public decimal? CostPrice { get; set; }

    /// <summary>理想买点：目标总市值（亿元人民币）。当前总市值(折人民币) 低于此值 → 可以击球。</summary>
    public decimal? IdealBuyMarketCapYi { get; set; }

    /// <summary>1 年内卖点：目标总市值（亿元人民币）。当前总市值(折人民币) 高于此值 → 提示可考虑卖出。</summary>
    public decimal? SellMarketCapYi { get; set; }
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

    /// <summary>内容时间窗（天）：只保留近 N 天（本周）的公告/财报/风险。周跟踪默认 7。</summary>
    public int LookbackDays { get; set; } = 7;

    /// <summary>港股例行公告排除关键词（英文，命中类型/标题即剔除，如每日披露/月报）。</summary>
    public List<string> HkExcludeTypes { get; set; } =
    [
        "Next Day Disclosure", "Monthly Return", "Proxy Form", "Notification Letter",
    ];
}

/// <summary>货币折算配置。</summary>
public sealed class CurrencyOptions
{
    /// <summary>基准货币（组合汇总与买卖点阈值口径）。本期固定人民币。</summary>
    public string BaseCurrency { get; set; } = "CNY";

    /// <summary>汇率抓取失败时的回退静态汇率（币种 → 折人民币），如 HKD→0.87。</summary>
    public Dictionary<string, decimal> StaticRates { get; set; } = new() { ["HKD"] = 0.87m };
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

    /// <summary>公告追踪配置。</summary>
    public AnnouncementOptions Announcements { get; set; } = new();

    /// <summary>风险关键词（命中公告标题即在风险提示区高亮）。</summary>
    public List<string> RiskKeywords { get; set; } =
    [
        "减持", "质押", "冻结", "诉讼", "仲裁", "处罚", "违规", "ST", "退市",
        "问询", "商誉", "停牌", "下调评级", "立案", "风险警示",
    ];

    /// <summary>货币折算配置。</summary>
    public CurrencyOptions Currency { get; set; } = new();

    /// <summary>报告输出配置。</summary>
    public ReportOptions Report { get; set; } = new();
}
