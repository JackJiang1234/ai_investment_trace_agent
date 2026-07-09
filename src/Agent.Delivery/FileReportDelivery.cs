using System.Globalization;
using System.Text;
using Agent.Core;

namespace Agent.Delivery;

/// <summary>
/// 默认投递方式：将报告写入输出目录，文件名 <c>YYYY-MM-DD.{ext}</c>。
/// GitHub Actions 后续将该目录提交回仓库。
/// </summary>
public sealed class FileReportDelivery : IReportDelivery
{
    private static readonly IReadOnlyDictionary<ReportFormat, string> Extensions =
        new Dictionary<ReportFormat, string>
        {
            [ReportFormat.Markdown] = "md",
            [ReportFormat.Html] = "html",
        };

    private readonly string _outputDirectory;

    public FileReportDelivery(string outputDirectory)
    {
        _outputDirectory = outputDirectory;
    }

    public async Task DeliverAsync(RenderedReport report, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_outputDirectory);
        var date = report.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        foreach (var (format, content) in report.Contents)
        {
            var ext = Extensions.TryGetValue(format, out var e) ? e : format.ToString().ToLowerInvariant();
            var path = Path.Combine(_outputDirectory, $"{date}.{ext}");
            await File.WriteAllTextAsync(path, content, new UTF8Encoding(false), cancellationToken);
        }
    }
}
