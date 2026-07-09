using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Agent.Core;
using Scriban;
using Scriban.Runtime;

namespace Agent.Reporting;

/// <summary>
/// 用 Scriban 模板将 <see cref="DailyReport"/> 渲染为 Markdown / HTML。
/// 模板以嵌入资源随程序集分发，首次使用时解析并缓存。
/// </summary>
public sealed class ScribanReportRenderer : IReportRenderer
{
    private static readonly IReadOnlyDictionary<ReportFormat, string> ResourceSuffix =
        new Dictionary<ReportFormat, string>
        {
            [ReportFormat.Markdown] = "report.md.sbn",
            [ReportFormat.Html] = "report.html.sbn",
        };

    private readonly ConcurrentDictionary<ReportFormat, Template> _cache = new();

    public string Render(DailyReport report, ReportFormat format)
    {
        var template = _cache.GetOrAdd(format, LoadTemplate);

        // 用 m => m.Name 保持模板中的成员名与 C# 属性一致（PascalCase）。
        var root = new ScriptObject();
        root.Import(report, renamer: m => m.Name);
        root.SetValue("DateText", report.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), true);

        var context = new TemplateContext { MemberRenamer = m => m.Name };
        context.PushGlobal(root);

        return template.Render(context);
    }

    private static Template LoadTemplate(ReportFormat format)
    {
        var assembly = typeof(ScribanReportRenderer).Assembly;
        var suffix = ResourceSuffix[format];
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"未找到报告模板嵌入资源：{suffix}");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();

        var template = Template.Parse(text, resourceName);
        if (template.HasErrors)
        {
            throw new InvalidOperationException(
                $"报告模板解析失败（{suffix}）：{string.Join("; ", template.Messages)}");
        }

        return template;
    }
}
