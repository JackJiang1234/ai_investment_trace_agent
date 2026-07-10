using Agent.DataSource;
using FluentAssertions;

namespace Agent.DataSource.Tests;

/// <summary>
/// 打真实东财外汇接口的集成测试。默认单测以 <c>--filter "Category!=Integration"</c> 排除。
/// </summary>
[Trait("Category", "Integration")]
public class EastMoneyExchangeRateSourceIntegrationTests
{
    [Fact]
    public async Task GetHkdToCnyAsync_RealEndpoint_ReturnsPlausibleRate()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (AiInvestmentTraceAgent)");
        var source = new EastMoneyExchangeRateSource(client);

        var rate = await source.GetHkdToCnyAsync();

        rate.Should().NotBeNull();
        // 港元兑人民币历史区间大致 0.8–1.0，用宽松边界防脆弱。
        rate!.Value.Should().BeInRange(0.7m, 1.1m);
    }
}
