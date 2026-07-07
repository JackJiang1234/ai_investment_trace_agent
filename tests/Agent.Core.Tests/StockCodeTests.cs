using Agent.Core;
using FluentAssertions;

namespace Agent.Core.Tests;

public class StockCodeTests
{
    [Fact]
    public void Parse_ShanghaiCode_SetsMarketAndEastMoneySecId()
    {
        var code = StockCode.Parse("600519.SH");

        code.Market.Should().Be(Market.SH);
        code.Symbol.Should().Be("600519");
        code.EastMoneySecId.Should().Be("1.600519");
    }

    [Fact]
    public void Parse_ShenzhenCode_SetsMarketAndEastMoneySecId()
    {
        var code = StockCode.Parse("000001.SZ");

        code.Market.Should().Be(Market.SZ);
        code.Symbol.Should().Be("000001");
        code.EastMoneySecId.Should().Be("0.000001");
    }

    [Fact]
    public void Parse_HongKongCode_SetsMarketAndEastMoneySecId()
    {
        var code = StockCode.Parse("00700.HK");

        code.Market.Should().Be(Market.HK);
        code.Symbol.Should().Be("00700");
        code.EastMoneySecId.Should().Be("116.00700");
    }

    [Fact]
    public void Parse_IsCaseInsensitiveAndTrimsWhitespace()
    {
        var code = StockCode.Parse("  600519.sh  ");

        code.Market.Should().Be(Market.SH);
        code.EastMoneySecId.Should().Be("1.600519");
    }

    [Theory]
    [InlineData("")]
    [InlineData("600519")]        // 缺少市场后缀
    [InlineData("600519.XX")]     // 未知市场
    [InlineData(".SH")]           // 缺少代码
    [InlineData("600519.SH.SZ")]  // 多余分隔
    public void Parse_InvalidInput_ThrowsFormatException(string input)
    {
        var act = () => StockCode.Parse(input);

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Parse_NullInput_ThrowsFormatException()
    {
        var act = () => StockCode.Parse(null!);

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void ToString_ReturnsNormalizedCanonicalCode()
    {
        StockCode.Parse("600519.sh").ToString().Should().Be("600519.SH");
    }
}
