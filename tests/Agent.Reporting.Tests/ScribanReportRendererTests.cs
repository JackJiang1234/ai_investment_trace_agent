using Agent.Core;
using Agent.Reporting;
using FluentAssertions;

namespace Agent.Reporting.Tests;

public class ScribanReportRendererTests
{
    private readonly ScribanReportRenderer _renderer = new();

    [Fact]
    public void Render_Markdown_ContainsDateStocksAndSummary()
    {
        var md = _renderer.Render(TestReports.Sample(), ReportFormat.Markdown);

        md.Should().Contain("2026-07-08");
        md.Should().Contain("贵州茅台");
        md.Should().Contain("腾讯控股");
        md.Should().Contain("0.88");   // 茅台涨跌幅
        md.Should().Contain("6.3");    // 腾讯涨跌幅
    }

    [Fact]
    public void Render_Markdown_HighlightsAlertedStock()
    {
        var md = _renderer.Render(TestReports.Sample(), ReportFormat.Markdown);

        // 异动提醒区应出现命中描述
        md.Should().Contain("涨幅 6.3% ≥ 5%");
    }

    [Fact]
    public void Render_Html_IsWellFormedAndContainsData()
    {
        var html = _renderer.Render(TestReports.Sample(), ReportFormat.Html);

        html.Should().Contain("<html");
        html.Should().Contain("</html>");
        html.Should().Contain("贵州茅台");
        html.Should().Contain("腾讯控股");
        html.Should().Contain("2026-07-08");
    }

    [Fact]
    public void Render_ShowsPlaceholderForNullIndicator()
    {
        // 茅台 MA20 为 null → 应显示占位符而非空白/报错
        var md = _renderer.Render(TestReports.Sample(), ReportFormat.Markdown);

        md.Should().Contain("—");
    }
}
