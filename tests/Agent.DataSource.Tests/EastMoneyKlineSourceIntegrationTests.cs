using Agent.Core;
using FluentAssertions;

namespace Agent.DataSource.Tests;

/// <summary>
/// 打真实东财K线接口的集成测试。默认单测以 <c>--filter "Category!=Integration"</c> 排除。
/// </summary>
[Trait("Category", "Integration")]
public class EastMoneyKlineSourceIntegrationTests
{
    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (AiInvestmentTraceAgent)");
        return client;
    }

    [Theory]
    [InlineData("600519.SH")]
    [InlineData("00700.HK")]
    public async Task GetDailyBarsAsync_RealEndpoint_ReturnsAscendingBars(string code)
    {
        using var client = CreateClient();
        var source = new EastMoneyKlineSource(client);

        var bars = await source.GetDailyBarsAsync(StockCode.Parse(code), 30);

        bars.Should().NotBeEmpty();
        bars.Should().BeInAscendingOrder(b => b.Date);
        bars[^1].Close.Should().BeGreaterThan(0m);
    }

    [Theory]
    [InlineData("600519.SH")]
    [InlineData("00700.HK")]
    public async Task GetYearStartCloseAsync_RealEndpoint_ReturnsPositiveClose(string code)
    {
        using var client = CreateClient();
        var source = new EastMoneyKlineSource(client);

        var close = await source.GetYearStartCloseAsync(StockCode.Parse(code), 2026);

        close.Should().NotBeNull();
        close!.Value.Should().BeGreaterThan(0m);
    }
}
