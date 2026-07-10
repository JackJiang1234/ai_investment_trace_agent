using Agent.Core;
using FluentAssertions;

namespace Agent.Core.Tests;

public class RiskKeywordMatcherTests
{
    private static readonly string[] Keywords = ["减持", "质押", "诉讼", "ST"];

    [Fact]
    public void FindMatch_ReturnsFirstMatchedKeyword()
    {
        RiskKeywordMatcher.FindMatch("关于控股股东股份质押的公告", Keywords)
            .Should().Be("质押");
    }

    [Fact]
    public void FindMatch_RespectsKeywordOrder()
    {
        // 标题同时含"减持"和"质押"，应返回关键词列表中靠前的"减持"
        RiskKeywordMatcher.FindMatch("股东减持及质押公告", Keywords).Should().Be("减持");
    }

    [Fact]
    public void FindMatch_NoMatch_ReturnsNull()
    {
        RiskKeywordMatcher.FindMatch("正常经营公告", Keywords).Should().BeNull();
    }

    [Fact]
    public void FindMatch_EmptyInputs_ReturnsNull()
    {
        RiskKeywordMatcher.FindMatch("", Keywords).Should().BeNull();
        RiskKeywordMatcher.FindMatch("减持", []).Should().BeNull();
    }
}
