using Agent.DataSource;
using FluentAssertions;

namespace Agent.DataSource.Tests;

public class EastMoneyFinancialParserTests
{
    private static string ReadFixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public void ParseSnapshot_Fixture_MapsLatestReport()
    {
        var s = EastMoneyFinancialParser.ParseSnapshot(ReadFixture("eastmoney_fin_600519.json"));

        s.Should().NotBeNull();
        s!.ReportPeriod.Should().Be(new DateOnly(2026, 3, 31));
        s.PeriodLabel.Should().Be("2026年 一季报");
        s.NoticeDate.Should().Be(new DateOnly(2026, 4, 25));
        s.Revenue.Should().Be(54702912385.23m);
        s.NetProfit.Should().Be(27242512886.45m);
        s.RevenueYoY.Should().BeApproximately(6.336m, 0.001m);
        s.NetProfitYoY.Should().Be(1.47m);
        s.Roe.Should().Be(10.57m);
        s.GrossMargin.Should().BeApproximately(89.759m, 0.001m);
        s.Eps.Should().Be(21.76m);
    }

    [Fact]
    public void ParseSnapshot_RevenueInYi_Computed()
    {
        var s = EastMoneyFinancialParser.ParseSnapshot(ReadFixture("eastmoney_fin_600519.json"));

        s!.RevenueInYi.Should().BeApproximately(547.03m, 0.01m);
    }

    [Fact]
    public void ParseForecast_Fixture_MapsLatest()
    {
        var f = EastMoneyFinancialParser.ParseForecast(ReadFixture("eastmoney_forecast_600519.json"));

        f.Should().NotBeNull();
        f!.NoticeDate.Should().Be(new DateOnly(2025, 1, 3));
        f.ReportPeriod.Should().Be(new DateOnly(2024, 12, 31));
        f.Type.Should().Be("略增");
        f.Content.Should().Contain("营业收入");
    }

    [Fact]
    public void Parse_EmptyResult_ReturnsNull()
    {
        var empty = ReadFixture("eastmoney_fin_empty.json");

        EastMoneyFinancialParser.ParseSnapshot(empty).Should().BeNull();
        EastMoneyFinancialParser.ParseForecast(empty).Should().BeNull();
    }

    [Fact]
    public void Parse_GarbageOrEmpty_ReturnsNull()
    {
        EastMoneyFinancialParser.ParseSnapshot("").Should().BeNull();
        EastMoneyFinancialParser.ParseSnapshot("not json").Should().BeNull();
        EastMoneyFinancialParser.ParseForecast("{}").Should().BeNull();
    }
}
