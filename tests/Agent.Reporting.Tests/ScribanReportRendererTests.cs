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
    public void Render_FootnoteShowsExchangeRate()
    {
        var html = _renderer.Render(TestReports.Sample(), ReportFormat.Html);

        html.Should().Contain("1 HKD").And.Contain("0.87");
    }
}
