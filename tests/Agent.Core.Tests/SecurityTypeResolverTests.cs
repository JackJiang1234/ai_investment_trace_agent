using Agent.Core;
using FluentAssertions;

namespace Agent.Core.Tests;

public class SecurityTypeResolverTests
{
    [Theory]
    [InlineData("515170.SH", SecurityType.Etf)]  // 沪 ETF
    [InlineData("588000.SH", SecurityType.Etf)]  // 科创50 ETF
    [InlineData("563000.SH", SecurityType.Etf)]
    [InlineData("159915.SZ", SecurityType.Etf)]  // 深 ETF
    [InlineData("161725.SZ", SecurityType.Etf)]  // 深 LOF
    [InlineData("600519.SH", SecurityType.Stock)]
    [InlineData("002027.SZ", SecurityType.Stock)]
    [InlineData("00700.HK", SecurityType.Stock)] // 港股一律按股票
    public void Detect_ByCodePrefix(string code, SecurityType expected)
    {
        SecurityTypeResolver.Detect(StockCode.Parse(code)).Should().Be(expected);
    }

    [Fact]
    public void Resolve_ExplicitConfigOverridesDetection()
    {
        var config = new StockConfig { Code = "515170.SH", SecurityType = SecurityType.Stock };

        SecurityTypeResolver.Resolve(config, StockCode.Parse("515170.SH"))
            .Should().Be(SecurityType.Stock);
    }

    [Fact]
    public void Resolve_FallsBackToDetection_WhenUnset()
    {
        var config = new StockConfig { Code = "515170.SH" };

        SecurityTypeResolver.Resolve(config, StockCode.Parse("515170.SH"))
            .Should().Be(SecurityType.Etf);
    }
}
