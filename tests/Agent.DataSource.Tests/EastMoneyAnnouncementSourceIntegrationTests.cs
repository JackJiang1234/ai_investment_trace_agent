using Agent.Core;
using FluentAssertions;

namespace Agent.DataSource.Tests;

/// <summary>打真实东财公告接口的集成测试。默认单测以 Category!=Integration 排除。</summary>
[Trait("Category", "Integration")]
public class EastMoneyAnnouncementSourceIntegrationTests
{
    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (AiInvestmentTraceAgent)");
        return client;
    }

    [Fact]
    public async Task GetRecent_AShare_ReturnsAnnouncements()
    {
        using var client = CreateClient();
        var source = new EastMoneyAnnouncementSource(client);

        var anns = await source.GetRecentAnnouncementsAsync(StockCode.Parse("600519.SH"), 5);

        anns.Should().NotBeEmpty();
        anns[0].Title.Should().NotBeNullOrWhiteSpace();
        anns[0].Url.Should().StartWith("https://data.eastmoney.com/notices/detail/");
    }

    [Fact]
    public async Task GetRecent_HongKong_ReturnsEmpty_UntilM5b()
    {
        // 记录当前行为：东财公告接口不返回港股数据，港股公告留待 M5b（披露易）。
        using var client = CreateClient();
        var source = new EastMoneyAnnouncementSource(client);

        var anns = await source.GetRecentAnnouncementsAsync(StockCode.Parse("00700.HK"), 5);

        anns.Should().BeEmpty();
    }
}
