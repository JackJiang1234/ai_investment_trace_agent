using Agent.Core;
using FluentAssertions;

namespace Agent.DataSource.Tests;

/// <summary>打真实东财资金流接口的集成测试。默认单测以 Category!=Integration 排除。</summary>
[Trait("Category", "Integration")]
public class EastMoneyFundFlowSourceIntegrationTests
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
    public async Task GetLatestFundFlowAsync_RealEndpoint_ReturnsConsistentFlow(string code)
    {
        using var client = CreateClient();
        var source = new EastMoneyFundFlowSource(client);

        var flow = await source.GetLatestFundFlowAsync(StockCode.Parse(code));

        flow.Should().NotBeNull();
        // 主力 = 大单 + 超大单（结构恒等式）
        (flow!.LargeNetInflow + flow.SuperLargeNetInflow).Should().Be(flow.MainNetInflow);
    }
}
