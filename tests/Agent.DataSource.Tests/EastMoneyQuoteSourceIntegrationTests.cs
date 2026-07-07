using Agent.Core;
using FluentAssertions;

namespace Agent.DataSource.Tests;

/// <summary>
/// 打真实东财接口的集成测试。默认单测运行应以 <c>--filter "Category!=Integration"</c> 排除，
/// 仅在需要验证真实连通性时运行。
/// </summary>
[Trait("Category", "Integration")]
public class EastMoneyQuoteSourceIntegrationTests
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
    public async Task GetQuoteAsync_RealEndpoint_ReturnsPlausibleQuote(string code)
    {
        using var client = CreateClient();
        var source = new EastMoneyQuoteSource(client);

        var quote = await source.GetQuoteAsync(StockCode.Parse(code));

        quote.Should().NotBeNull();
        quote!.Name.Should().NotBeNullOrWhiteSpace();
        quote.Price.Should().BeGreaterThan(0m);
        quote.PreviousClose.Should().BeGreaterThan(0m);
    }
}
