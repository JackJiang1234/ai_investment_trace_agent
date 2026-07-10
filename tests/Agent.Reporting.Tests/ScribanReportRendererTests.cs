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
        md.Should().Contain("组合概览");
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
}
