using Agent.Core;
using FluentAssertions;

namespace Agent.Core.Tests;

public class AnnouncementFilterTests
{
    private static Announcement Ann(string title, string type) => new()
    {
        Date = new DateOnly(2026, 7, 1),
        Title = title,
        Type = type,
        Url = "https://x",
    };

    [Fact]
    public void FilterImportant_KeepsOnlyMatchingTypeOrTitle()
    {
        var options = new AnnouncementOptions
        {
            ImportantTypesOnly = true,
            IncludeTypes = ["业绩", "减持"],
        };
        var anns = new[]
        {
            Ann("XX业绩预告", "业绩预告"),          // 类型命中
            Ann("关于股东减持股份的公告", "股东行为"), // 标题命中
            Ann("关于办公地址变更的公告", "日常经营"), // 不命中
        };

        var result = AnnouncementFilter.FilterImportant(anns, options);

        result.Should().HaveCount(2);
        result.Should().NotContain(a => a.Title.Contains("办公地址"));
    }

    [Fact]
    public void FilterImportant_WhenDisabled_KeepsAll()
    {
        var options = new AnnouncementOptions { ImportantTypesOnly = false, IncludeTypes = ["业绩"] };
        var anns = new[] { Ann("日常公告", "日常"), Ann("业绩公告", "业绩") };

        AnnouncementFilter.FilterImportant(anns, options).Should().HaveCount(2);
    }

    [Fact]
    public void FilterImportant_EmptyIncludeTypes_KeepsNone_WhenEnabled()
    {
        var options = new AnnouncementOptions { ImportantTypesOnly = true, IncludeTypes = [] };

        AnnouncementFilter.FilterImportant([Ann("业绩", "业绩")], options).Should().BeEmpty();
    }
}
