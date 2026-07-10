namespace Agent.Core;

/// <summary>报告中的一个分组（如"核心持仓"/"观察池"）及其个股。</summary>
public sealed record ReportGroup
{
    /// <summary>分组名。</summary>
    public required string Name { get; init; }

    /// <summary>该分组下的个股（按配置顺序）。</summary>
    public required IReadOnlyList<StockAnalysis> Stocks { get; init; }
}

/// <summary>某一期（周跟踪）的完整追踪报告数据模型（渲染前的中间产物）。</summary>
public sealed record DailyReport
{
    // 分组展示顺序：核心持仓在前，观察池次之，其余按出现顺序。
    private static readonly string[] GroupOrder = ["核心持仓", "观察池"];

    /// <summary>报告日期（当周周五）。</summary>
    public required DateOnly Date { get; init; }

    /// <summary>组合概览。</summary>
    public required PortfolioSummary Summary { get; init; }

    /// <summary>全部个股分析（按配置顺序）。</summary>
    public required IReadOnlyList<StockAnalysis> Stocks { get; init; }

    /// <summary>命中风险关键词的公告（风险提示区，跨个股汇总）。</summary>
    public IReadOnlyList<RiskHit> RiskHits { get; init; } = [];

    /// <summary>所用 HKD→CNY 汇率（报告脚注标注）。</summary>
    public decimal HkdToCny { get; init; }

    /// <summary>汇率是否取自实时源（false 表示回退静态值）。</summary>
    public bool IsLiveRate { get; init; }

    /// <summary>AI 摘要（默认关闭时为 null）。</summary>
    public string? AiSummary { get; init; }

    /// <summary>命中"可以击球"的个股（买点提示区，跨分组置顶）。</summary>
    public IReadOnlyList<StockAnalysis> Buyable =>
        Stocks.Where(s => s.Holding?.CanBuy == true).ToArray();

    /// <summary>按分组归类的个股（核心持仓在前、观察池次之，其余按出现顺序）。</summary>
    public IReadOnlyList<ReportGroup> Groups
    {
        get
        {
            var order = new List<string>();
            var byGroup = new Dictionary<string, List<StockAnalysis>>();
            foreach (var s in Stocks)
            {
                var name = s.Holding?.Group is { Length: > 0 } g ? g : "其他";
                if (!byGroup.TryGetValue(name, out var list))
                {
                    list = [];
                    byGroup[name] = list;
                    order.Add(name);
                }

                list.Add(s);
            }

            return order
                .OrderBy(GroupRank)
                .ThenBy(order.IndexOf)
                .Select(name => new ReportGroup { Name = name, Stocks = byGroup[name] })
                .ToArray();
        }
    }

    private static int GroupRank(string name)
    {
        var i = Array.IndexOf(GroupOrder, name);
        return i >= 0 ? i : GroupOrder.Length;
    }
}
