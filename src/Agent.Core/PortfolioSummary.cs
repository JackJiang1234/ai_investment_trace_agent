namespace Agent.Core;

/// <summary>组合当日概览：涨跌家数、平均涨跌幅、领涨/领跌个股。</summary>
public sealed record PortfolioSummary
{
    /// <summary>上涨家数（涨跌幅 &gt; 0）。</summary>
    public required int Advancers { get; init; }

    /// <summary>下跌家数（涨跌幅 &lt; 0）。</summary>
    public required int Decliners { get; init; }

    /// <summary>平盘家数（涨跌幅 == 0）。</summary>
    public required int Unchanged { get; init; }

    /// <summary>平均涨跌幅（%）。</summary>
    public required decimal AverageChangePercent { get; init; }

    /// <summary>领涨个股；空组合为 null。</summary>
    public StockAnalysis? TopGainer { get; init; }

    /// <summary>领跌个股；空组合为 null。</summary>
    public StockAnalysis? TopLoser { get; init; }

    /// <summary>由个股分析列表汇总组合概览。</summary>
    public static PortfolioSummary From(IReadOnlyList<StockAnalysis> stocks)
    {
        if (stocks.Count == 0)
        {
            return new PortfolioSummary
            {
                Advancers = 0,
                Decliners = 0,
                Unchanged = 0,
                AverageChangePercent = 0m,
            };
        }

        var advancers = 0;
        var decliners = 0;
        var unchanged = 0;
        decimal sum = 0;
        var top = stocks[0];
        var bottom = stocks[0];

        foreach (var s in stocks)
        {
            var pct = s.Quote.ChangePercent;
            sum += pct;
            if (pct > 0) advancers++;
            else if (pct < 0) decliners++;
            else unchanged++;

            if (pct > top.Quote.ChangePercent) top = s;
            if (pct < bottom.Quote.ChangePercent) bottom = s;
        }

        return new PortfolioSummary
        {
            Advancers = advancers,
            Decliners = decliners,
            Unchanged = unchanged,
            AverageChangePercent = sum / stocks.Count,
            TopGainer = top,
            TopLoser = bottom,
        };
    }
}
