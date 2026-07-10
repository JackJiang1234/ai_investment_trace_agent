using Agent.DataSource;
using FluentAssertions;

namespace Agent.DataSource.Tests;

public class EastMoneyAnnouncementParserTests
{
    private static string ReadFixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public void Parse_Fixture_MapsFirstAnnouncement()
    {
        var anns = EastMoneyAnnouncementParser.Parse(ReadFixture("eastmoney_ann_600519.json"));

        anns.Should().HaveCount(8);
        var first = anns[0];
        first.Date.Should().Be(new DateOnly(2026, 6, 22));
        first.Title.Should().Be("贵州茅台:贵州茅台2025年年度权益分派实施公告");
        first.Type.Should().Be("分配方案实施");
        first.Url.Should().Be("https://data.eastmoney.com/notices/detail/600519/AN202606211823708334.html");
    }

    [Fact]
    public void Parse_JoinsMultipleColumnNames()
    {
        var anns = EastMoneyAnnouncementParser.Parse(ReadFixture("eastmoney_ann_600519.json"));

        // fixture 中存在含两个 column_name 的公告：高管人员任职变动 + 董事会决议公告
        anns.Should().Contain(a => a.Type.Contains("高管人员任职变动") && a.Type.Contains("董事会决议公告"));
    }

    [Fact]
    public void Parse_EmptyList_ReturnsEmpty()
    {
        EastMoneyAnnouncementParser.Parse(ReadFixture("eastmoney_ann_empty.json")).Should().BeEmpty();
    }

    [Fact]
    public void Parse_GarbageOrEmpty_ReturnsEmpty()
    {
        EastMoneyAnnouncementParser.Parse("").Should().BeEmpty();
        EastMoneyAnnouncementParser.Parse("not json").Should().BeEmpty();
    }

    [Fact]
    public void Parse_SkipsItemsMissingRequiredFields()
    {
        var json = """
        {"data":{"list":[
          {"art_code":"AN1","codes":[{"stock_code":"600519"}],"columns":[{"column_name":"业绩预告"}],"notice_date":"2026-07-01 00:00:00","title_ch":"好公告"},
          {"art_code":"","codes":[],"columns":[],"notice_date":"bad","title_ch":""}
        ]}}
        """;

        var anns = EastMoneyAnnouncementParser.Parse(json);

        anns.Should().ContainSingle();
        anns[0].Title.Should().Be("好公告");
    }
}
