using Agent.Core;
using Agent.DataSource;
using FluentAssertions;

namespace Agent.DataSource.Tests;

public class EastMoneyQuoteParserTests
{
    private static string ReadFixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public void Parse_ShanghaiFixture_ScalesPricesByTwoDecimals()
    {
        var json = ReadFixture("eastmoney_600519.json");
        var code = StockCode.Parse("600519.SH");

        var quote = EastMoneyQuoteParser.Parse(json, code);

        quote.Should().NotBeNull();
        quote!.Code.Should().Be(code);
        quote.Name.Should().Be("贵州茅台");
        quote.Price.Should().Be(1188.80m);        // f43 118880 / 10^2
        quote.PreviousClose.Should().Be(1206.91m); // f60 120691 / 10^2
        quote.Open.Should().Be(1200.00m);          // f46
        quote.High.Should().Be(1202.00m);          // f44
        quote.Low.Should().Be(1188.11m);           // f45
        quote.ChangeAmount.Should().Be(-18.11m);   // f169 -1811 / 10^2
        quote.ChangePercent.Should().Be(-1.50m);   // f170 -150 / 100
        quote.Volume.Should().Be(27365);           // f47 原始
        quote.Turnover.Should().Be(3264967794.0m); // f48 原始
        quote.TurnoverRate.Should().Be(0.22m);     // f168 22 / 100
        quote.VolumeRatio.Should().Be(0.66m);      // f50 66 / 100
        quote.TotalMarketCap.Should().Be(1493368000000.00m); // f116 原始（元，不缩放）
    }

    [Fact]
    public void Parse_HongKongFixture_ScalesPricesByThreeDecimals()
    {
        var json = ReadFixture("eastmoney_00700.json");
        var code = StockCode.Parse("00700.HK");

        var quote = EastMoneyQuoteParser.Parse(json, code);

        quote.Should().NotBeNull();
        quote!.Name.Should().Be("腾讯控股");
        quote.Price.Should().Be(462.000m);          // f43 462000 / 10^3
        quote.PreviousClose.Should().Be(452.000m);  // f60 452000 / 10^3
        quote.ChangeAmount.Should().Be(10.000m);    // f169 10000 / 10^3
        quote.ChangePercent.Should().Be(2.21m);     // f170 221 / 100
        quote.VolumeRatio.Should().Be(1.50m);       // f50 150 / 100
        quote.TotalMarketCap.Should().Be(4184375996197.80m); // f116 原始（港元，不缩放）
    }

    [Fact]
    public void Parse_InvalidCodeFixture_ReturnsNull()
    {
        var json = ReadFixture("eastmoney_invalid.json");
        var code = StockCode.Parse("999999.SH");

        var quote = EastMoneyQuoteParser.Parse(json, code);

        quote.Should().BeNull();
    }

    [Fact]
    public void Parse_EmptyOrGarbageJson_ReturnsNull()
    {
        var code = StockCode.Parse("600519.SH");

        EastMoneyQuoteParser.Parse("", code).Should().BeNull();
        EastMoneyQuoteParser.Parse("not json", code).Should().BeNull();
    }
}
