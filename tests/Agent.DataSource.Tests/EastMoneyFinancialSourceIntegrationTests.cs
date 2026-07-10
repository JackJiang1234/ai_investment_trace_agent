using Agent.Core;
using FluentAssertions;

namespace Agent.DataSource.Tests;

/// <summary>打真实东财财报接口的集成测试。默认单测以 Category!=Integration 排除。</summary>
[Trait("Category", "Integration")]
public class EastMoneyFinancialSourceIntegrationTests
{
    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (AiInvestmentTraceAgent)");
        return client;
    }

    [Fact]
    public async Task GetFinancials_AShare_ReturnsSnapshot()
    {
        using var client = CreateClient();
        var source = new EastMoneyFinancialSource(client);

        var info = await source.GetFinancialsAsync(StockCode.Parse("600519.SH"));

        info.Should().NotBeNull();
        info!.Snapshot.Should().NotBeNull();
        info.Snapshot!.Revenue.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task GetFinancials_HongKong_ReturnsNull()
    {
        // 记录当前行为：东财 datacenter 无港股财务，港股财报本期不做。
        using var client = CreateClient();
        var source = new EastMoneyFinancialSource(client);

        var info = await source.GetFinancialsAsync(StockCode.Parse("00700.HK"));

        info.Should().BeNull();
    }
}
