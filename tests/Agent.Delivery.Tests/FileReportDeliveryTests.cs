using Agent.Core;
using Agent.Delivery;
using FluentAssertions;

namespace Agent.Delivery.Tests;

public class FileReportDeliveryTests : IDisposable
{
    private readonly string _dir = Path.Combine(
        Path.GetTempPath(), "agent-delivery-tests", Guid.NewGuid().ToString("N"));

    private RenderedReport Report() => new()
    {
        Date = new DateOnly(2026, 7, 8),
        Contents = new Dictionary<ReportFormat, string>
        {
            [ReportFormat.Html] = "<html>hi</html>",
            [ReportFormat.Markdown] = "# hi",
        },
    };

    [Fact]
    public async Task DeliverAsync_WritesOneFilePerFormat_WithDateAndExtension()
    {
        var delivery = new FileReportDelivery(_dir);

        await delivery.DeliverAsync(Report());

        var html = Path.Combine(_dir, "2026-07-08.html");
        var md = Path.Combine(_dir, "2026-07-08.md");
        File.Exists(html).Should().BeTrue();
        File.Exists(md).Should().BeTrue();
        (await File.ReadAllTextAsync(html)).Should().Be("<html>hi</html>");
        (await File.ReadAllTextAsync(md)).Should().Be("# hi");
    }

    [Fact]
    public async Task DeliverAsync_WeChatHtml_UsesWechatHtmlExtension()
    {
        var delivery = new FileReportDelivery(_dir);
        var report = Report() with
        {
            Contents = new Dictionary<ReportFormat, string> { [ReportFormat.WeChatHtml] = "<section>hi</section>" },
        };

        await delivery.DeliverAsync(report);

        File.Exists(Path.Combine(_dir, "2026-07-08.wechat.html")).Should().BeTrue();
    }

    [Fact]
    public async Task DeliverAsync_CreatesOutputDirectoryIfMissing()
    {
        Directory.Exists(_dir).Should().BeFalse();
        var delivery = new FileReportDelivery(_dir);

        await delivery.DeliverAsync(Report());

        Directory.Exists(_dir).Should().BeTrue();
    }

    [Fact]
    public async Task DeliverAsync_OverwritesExistingFile()
    {
        var delivery = new FileReportDelivery(_dir);
        await delivery.DeliverAsync(Report());

        var updated = Report() with
        {
            Contents = new Dictionary<ReportFormat, string> { [ReportFormat.Markdown] = "# updated" },
        };
        await delivery.DeliverAsync(updated);

        (await File.ReadAllTextAsync(Path.Combine(_dir, "2026-07-08.md")))
            .Should().Be("# updated");
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }
}
