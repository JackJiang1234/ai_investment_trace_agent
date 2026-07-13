using Agent.Core;
using Agent.Reporting;
using FluentAssertions;

namespace Agent.Reporting.Tests;

public class ScribanReportRendererTests
{
    private readonly ScribanReportRenderer _renderer = new();

    [Fact]
    public void Render_Markdown_ContainsDateGroupsAndSummary()
    {
        var md = _renderer.Render(TestReports.Sample(), ReportFormat.Markdown);

        md.Should().Contain("2026-07-10");
        md.Should().Contain("组合概览");
        md.Should().Contain("核心持仓").And.Contain("观察池"); // 分组
        md.Should().Contain("贵州茅台").And.Contain("腾讯控股");
        md.Should().Contain("年度收益"); // 组合概览新增年度收益金额/收益率
        md.Should().Contain("持仓总收益"); // 组合概览新增对成本收益
    }

    [Fact]
    public void Render_Markdown_ShowsHoldingFieldsForCore()
    {
        var md = _renderer.Render(TestReports.Sample(), ReportFormat.Markdown);

        md.Should().Contain("持股份数");     // 核心持仓有持仓明细
        md.Should().Contain("对成本盈亏");   // 核心持仓卡片显示对成本盈亏
        md.Should().Contain("可以击球");     // 茅台命中击球
    }

    [Fact]
    public void Render_Html_IsWellFormedWithGroupsAndBuyHighlight()
    {
        var html = _renderer.Render(TestReports.Sample(), ReportFormat.Html);

        html.Should().Contain("<html");
        html.Should().Contain("</html>");
        html.Should().Contain("核心持仓").And.Contain("观察池");
        html.Should().Contain("可以击球");
        html.Should().Contain("class=\"card hit\""); // 击球卡片红框
        html.Should().Contain("贵州茅台").And.Contain("腾讯控股");
    }

    [Fact]
    public void Render_WeChatHtml_UsesInlineStyles_NoStyleOrScriptTags()
    {
        var wx = _renderer.Render(TestReports.Sample(), ReportFormat.WeChatHtml);

        wx.Should().Contain("style=");            // 全内联样式
        wx.Should().NotContain("<style");         // 公众号会剥离
        wx.Should().NotContain("<script");
        wx.Should().NotContain("<head");
        wx.Should().NotContain("<html");
        wx.Should().Contain("可以击球");
        wx.Should().Contain("核心持仓").And.Contain("观察池");
    }

    [Fact]
    public void Render_NoAnnouncementsInGroup_ShowsPlaceholder()
    {
        // Sample 两只股票本周均无公告 → 每个分组显示占位提示。
        var md = _renderer.Render(TestReports.Sample(), ReportFormat.Markdown);
        var html = _renderer.Render(TestReports.Sample(), ReportFormat.Html);

        md.Should().Contain("本周无新增重要公告");
        html.Should().Contain("本周无新增重要公告");
    }

    [Fact]
    public void Render_GroupWithAnnouncement_OmitsPlaceholder()
    {
        var stock = new StockAnalysis
        {
            Quote = TestReports.Quote("600519.SH", "贵州茅台", 1200m),
            Holding = new HoldingMetrics { Group = "核心持仓", Currency = Currency.CNY },
            Announcements =
            [
                new Announcement { Date = new DateOnly(2026, 7, 10), Title = "分派实施公告", Type = "分派", Url = "https://x" },
            ],
        };
        IReadOnlyList<StockAnalysis> stocks = [stock];
        var report = new DailyReport
        {
            Date = new DateOnly(2026, 7, 10),
            Summary = PortfolioSummary.From(stocks),
            Stocks = stocks,
            HkdToCny = 0.87m,
        };

        var md = _renderer.Render(report, ReportFormat.Markdown);

        md.Should().Contain("分派实施公告");
        md.Should().NotContain("本周无新增重要公告");
    }

    [Fact]
    public void Render_EtfCard_ShowsFundSizeAndPriceThresholds_NotMarketCap()
    {
        var etf = new StockAnalysis
        {
            Quote = TestReports.Quote("515170.SH", "食品饮料ETF华夏", 0.439m),
            Holding = new HoldingMetrics
            {
                Group = "核心持仓",
                SecurityType = SecurityType.Etf,
                Currency = Currency.CNY,
                Shares = 309400,
                CostPrice = 0.56m,
                TotalMarketCapCnyYi = 40m,
                IdealBuyPrice = 0.50m,
                SellPrice = 0.75m,
                CanBuy = true,
            },
        };
        IReadOnlyList<StockAnalysis> stocks = [etf];
        var report = new DailyReport
        {
            Date = new DateOnly(2026, 7, 13),
            Summary = PortfolioSummary.From(stocks),
            Stocks = stocks,
            HkdToCny = 0.87m,
        };

        var html = _renderer.Render(report, ReportFormat.Html);

        html.Should().Contain("基金规模");       // ETF 用基金规模而非"当前市值"标签
        html.Should().Contain("理想买入价");     // 价格口径
        html.Should().Contain("可以击球");       // 0.439 < 0.50
        html.Should().NotContain("理想买点");    // 不应出现股票市值口径标签
    }

    [Fact]
    public void Render_FootnoteShowsExchangeRate()
    {
        var html = _renderer.Render(TestReports.Sample(), ReportFormat.Html);

        html.Should().Contain("1 HKD").And.Contain("0.87");
    }
}
