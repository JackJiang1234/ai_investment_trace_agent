using Agent.DataSource;
using FluentAssertions;

namespace Agent.DataSource.Tests;

public class EastMoneyFundFlowParserTests
{
    private static string ReadFixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public void Parse_ShanghaiFixture_ParsesLatestFlowInOrder()
    {
        var flows = EastMoneyFundFlowParser.Parse(ReadFixture("eastmoney_fflow_600519.json"));

        flows.Should().HaveCount(3);
        flows.Should().BeInAscendingOrder(f => f.Date);

        var last = flows[^1];
        last.Date.Should().Be(new DateOnly(2026, 7, 9));
        last.MainNetInflow.Should().Be(-388600352.0m);
        last.SmallNetInflow.Should().Be(-176247.0m);
        last.MediumNetInflow.Should().Be(388776592.0m);
        last.LargeNetInflow.Should().Be(4521744.0m);
        last.SuperLargeNetInflow.Should().Be(-393122096.0m);
    }

    [Fact]
    public void Parse_MainEqualsLargePlusSuperLarge()
    {
        var last = EastMoneyFundFlowParser.Parse(ReadFixture("eastmoney_fflow_00700.json"))[^1];

        (last.LargeNetInflow + last.SuperLargeNetInflow).Should().Be(last.MainNetInflow);
    }

    [Fact]
    public void Parse_InvalidFixture_ReturnsEmpty()
    {
        EastMoneyFundFlowParser.Parse(ReadFixture("eastmoney_fflow_invalid.json")).Should().BeEmpty();
    }

    [Fact]
    public void Parse_EmptyOrGarbage_ReturnsEmpty()
    {
        EastMoneyFundFlowParser.Parse("").Should().BeEmpty();
        EastMoneyFundFlowParser.Parse("not json").Should().BeEmpty();
    }

    [Fact]
    public void Parse_SkipsMalformedLines()
    {
        var json = """
        {"data":{"klines":[
          "2026-07-09,-388600352.0,-176247.0,388776592.0,4521744.0,-393122096.0",
          "bad,row",
          "not-a-date,1,2,3,4"
        ]}}
        """;

        var flows = EastMoneyFundFlowParser.Parse(json);

        flows.Should().ContainSingle();
        flows[0].MainNetInflow.Should().Be(-388600352.0m);
    }
}
