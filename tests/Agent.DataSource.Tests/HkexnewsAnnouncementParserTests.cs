using Agent.DataSource;
using FluentAssertions;

namespace Agent.DataSource.Tests;

public class HkexnewsAnnouncementParserTests
{
    private static string ReadFixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public void ParseStockId_FromPrefixResponse_ReturnsId()
    {
        var prefix = ReadFixture("hkexnews_prefix_00700.txt");

        HkexnewsAnnouncementParser.ParseStockId(prefix).Should().Be(7609);
    }

    [Fact]
    public void ParseStockId_Garbage_ReturnsNull()
    {
        HkexnewsAnnouncementParser.ParseStockId("").Should().BeNull();
        HkexnewsAnnouncementParser.ParseStockId("callback();").Should().BeNull();
        HkexnewsAnnouncementParser.ParseStockId("nonsense").Should().BeNull();
    }

    [Fact]
    public void Parse_SearchResponse_MapsFirstRecord()
    {
        var anns = HkexnewsAnnouncementParser.Parse(ReadFixture("hkexnews_search_00700.json"));

        anns.Should().NotBeEmpty();
        var first = anns[0];
        first.Title.Should().Be("Next Day Disclosure Return");
        first.Date.Should().Be(new DateOnly(2026, 7, 9));
        first.Type.Should().Be("Next Day Disclosure Returns - [Share Buyback]");
        first.Url.Should().Be("https://www1.hkexnews.hk/listedco/listconews/sehk/2026/0709/2026070900827.pdf");
    }

    [Fact]
    public void Parse_DecodesHtmlEntities()
    {
        var anns = HkexnewsAnnouncementParser.Parse(ReadFixture("hkexnews_search_00700.json"));

        // 第二条 LONG_TEXT 含 &#x2f; → 应解码为 "/"
        anns.Should().Contain(a => a.Type.Contains("Others / Share Buyback"));
        // 不应残留 <br/> 或转义实体
        anns.Should().OnlyContain(a => !a.Type.Contains("<br") && !a.Type.Contains("&#x"));
    }

    [Fact]
    public void Parse_GarbageOrEmpty_ReturnsEmpty()
    {
        HkexnewsAnnouncementParser.Parse("").Should().BeEmpty();
        HkexnewsAnnouncementParser.Parse("not json").Should().BeEmpty();
        HkexnewsAnnouncementParser.Parse("{\"result\":\"[]\"}").Should().BeEmpty();
    }
}
