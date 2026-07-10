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
    }

    [Fact]
    public void Render_Markdown_ShowsHoldingFieldsForCore()
    {
        var md = _renderer.Render(TestReports.Sample(), ReportFormat.Markdown);

        md.Should().Contain("持股份数");     // 核心持仓有持仓明细
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
    public void Render_FootnoteShowsExchangeRate()
    {
        var html = _renderer.Render(TestReports.Sample(), ReportFormat.Html);

        html.Should().Contain("1 HKD").And.Contain("0.87");
    }
}
