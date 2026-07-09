using Agent.DataSource;
using FluentAssertions;

namespace Agent.DataSource.Tests;

public class EastMoneyKlineParserTests
{
    private static string ReadFixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public void Parse_ShanghaiFixture_ReturnsBarsInDateOrder()
    {
        var json = ReadFixture("eastmoney_kline_600519.json");

        var bars = EastMoneyKlineParser.Parse(json);

        bars.Should().HaveCount(5);
        bars[0].Date.Should().Be(new DateOnly(2026, 7, 2));
        bars[^1].Date.Should().Be(new DateOnly(2026, 7, 8));

        var last = bars[^1];
        last.Open.Should().Be(1188.77m);
        last.Close.Should().Be(1199.30m);
        last.High.Should().Be(1200.98m);
        last.Low.Should().Be(1177.00m);
        last.Volume.Should().Be(25776);
        last.Amount.Should().Be(3071933498.00m);
        last.ChangePercent.Should().Be(0.88m);
        last.TurnoverRate.Should().Be(0.21m);
    }

    [Fact]
    public void Parse_HongKongFixture_ParsesDecimalPricesDirectly()
    {
        var json = ReadFixture("eastmoney_kline_00700.json");

        var bars = EastMoneyKlineParser.Parse(json);

        bars.Should().HaveCount(5);
        var last = bars[^1];
        last.Date.Should().Be(new DateOnly(2026, 7, 8));
        last.Close.Should().Be(476.400m);
        last.ChangePercent.Should().Be(3.30m);
    }

    [Fact]
    public void Parse_InvalidCodeFixture_ReturnsEmpty()
    {
        var json = ReadFixture("eastmoney_kline_invalid.json");

        EastMoneyKlineParser.Parse(json).Should().BeEmpty();
    }

    [Fact]
    public void Parse_EmptyOrGarbage_ReturnsEmpty()
    {
        EastMoneyKlineParser.Parse("").Should().BeEmpty();
        EastMoneyKlineParser.Parse("not json").Should().BeEmpty();
    }

    [Fact]
    public void Parse_SkipsMalformedLinesWithoutThrowing()
    {
        // 一行字段不足、一行日期非法，均应被跳过，正常行保留。
        var json = """
        {"data":{"klines":[
          "2026-07-08,1188.77,1199.30,1200.98,1177.00,25776,3071933498.00,2.02,0.88,10.50,0.21",
          "bad,line,too,few",
          "not-a-date,1,2,3,4,5,6,7,8,9,10"
        ]}}
        """;

        var bars = EastMoneyKlineParser.Parse(json);

        bars.Should().HaveCount(1);
        bars[0].Close.Should().Be(1199.30m);
    }
}
