using Agent.Core;
using Agent.DataSource;
using Agent.Delivery;
using Agent.Reporting;
using Agent.Summarizer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// 入口：读取配置 → 组装 DI → 生成当日追踪报告并写文件。
// ContentRootPath 固定到程序目录，使 appsettings.json 不依赖工作目录；
// 而报告输出目录为相对路径，落在运行时工作目录（仓库根）下，便于 Actions 提交回仓库。
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    ContentRootPath = AppContext.BaseDirectory,
    Args = args,
});

var options = builder.Configuration.GetSection(AgentOptions.SectionName).Get<AgentOptions>()
              ?? new AgentOptions();
builder.Services.AddSingleton(options);

static void ConfigureHttp(HttpClient client)
{
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (AiInvestmentTraceAgent)");
}

builder.Services.AddHttpClient<IQuoteSource, EastMoneyQuoteSource>(ConfigureHttp);
builder.Services.AddHttpClient<IKlineSource, EastMoneyKlineSource>(ConfigureHttp);
builder.Services.AddSingleton<IReportRenderer, ScribanReportRenderer>();
builder.Services.AddSingleton<IReportDelivery>(_ => new FileReportDelivery(options.Report.OutputDirectory));
builder.Services.AddSingleton<ISummarizer, NoOpSummarizer>();
builder.Services.AddTransient<ReportOrchestrator>();

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var orchestrator = host.Services.GetRequiredService<ReportOrchestrator>();

// 报告日期取北京时间（UTC+8）当日；收盘后运行即当日收盘数据。
var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(8));
var dateText = today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
logger.LogInformation("开始生成 {Date} 追踪报告，共 {Count} 只股票……", dateText, options.Stocks.Count);

var report = await orchestrator.RunAsync(today);

if (report.Stocks.Count == 0)
{
    logger.LogError("未取到任何股票数据，报告为空（可能是网络/数据源问题）。");
    return 1;
}

logger.LogInformation("报告已写入 {Dir}/{Date}.*（{Count} 只，其中异动 {Alerted} 只）。",
    options.Report.OutputDirectory, dateText, report.Stocks.Count, report.Alerted.Count);
return 0;

// 供 ILogger<Program> 与测试引用（顶层语句的隐式入口类）。
public partial class Program;
