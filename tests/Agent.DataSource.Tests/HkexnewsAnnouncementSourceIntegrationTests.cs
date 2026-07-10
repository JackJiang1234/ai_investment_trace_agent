using Agent.Core;
using FluentAssertions;

namespace Agent.DataSource.Tests;

/// <summary>打真实披露易接口的集成测试（两步：prefix + search）。默认单测以 Category!=Integration 排除。</summary>
[Trait("Category", "Integration")]
public class HkexnewsAnnouncementSourceIntegrationTests
{
    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (AiInvestmentTraceAgent)");
        return client;
    }

    [Fact]
    public async Task GetRecent_HongKong_ReturnsAnnouncements()
    {
        using var client = CreateClient();
        var source = new HkexnewsAnnouncementSource(client);

        var anns = await source.GetRecentAnnouncementsAsync(StockCode.Parse("00700.HK"), 10);

        anns.Should().NotBeEmpty();
        anns.Should().HaveCountLessThanOrEqualTo(10);
        anns[0].Title.Should().NotBeNullOrWhiteSpace();
        anns[0].Url.Should().StartWith("https://www1.hkexnews.hk/");
    }
}
