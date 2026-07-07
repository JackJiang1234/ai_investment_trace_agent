using System.Diagnostics;
using System.Globalization;
using Agent.Core;
using Agent.DataSource;

// 里程碑 1：最小连通性验证。
// 从（海外）运行环境实拉 A股 + 港股行情，并探测公告主机可达性。
// 任一行情拉取失败 → 非零退出码，让 CI 任务状态反映连通性。

string[] symbols = ["600519.SH", "00700.HK"];
(string Label, string Url)[] hostProbes =
[
    ("巨潮资讯 cninfo", "http://www.cninfo.com.cn"),
    ("港交所披露易 hkexnews", "https://www1.hkexnews.hk"),
];

using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (AiInvestmentTraceAgent)");

var quoteSource = new EastMoneyQuoteSource(httpClient);
var ci = CultureInfo.InvariantCulture;
var hadFailure = false;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("=== 行情连通性验证（东方财富）===");
Console.WriteLine($"{"代码",-11}{"名称",-10}{"现价",12}{"涨跌幅",10}{"成交量",14}");
Console.WriteLine(new string('-', 60));

foreach (var symbol in symbols)
{
    var code = StockCode.Parse(symbol);
    try
    {
        var q = await quoteSource.GetQuoteAsync(code);
        if (q is null)
        {
            hadFailure = true;
            Console.WriteLine($"{symbol,-11}<无数据/停牌或无效代码>");
            continue;
        }

        Console.WriteLine(string.Format(
            ci,
            "{0,-11}{1,-10}{2,12:N2}{3,9:N2}%{4,14:N0}",
            q.Code, q.Name, q.Price, q.ChangePercent, q.Volume));
    }
    catch (Exception ex)
    {
        hadFailure = true;
        Console.WriteLine($"{symbol,-11}拉取失败：{ex.Message}");
    }
}

Console.WriteLine();
Console.WriteLine("=== 公告主机可达性探测 ===");
foreach (var (label, url) in hostProbes)
{
    var sw = Stopwatch.StartNew();
    try
    {
        using var resp = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        sw.Stop();
        Console.WriteLine($"{label,-22} → HTTP {(int)resp.StatusCode} ({sw.ElapsedMilliseconds} ms)");
    }
    catch (Exception ex)
    {
        // 探测失败仅告警，不影响退出码（本里程碑主目标是行情连通性）。
        sw.Stop();
        Console.WriteLine($"{label,-22} → 不可达：{ex.Message} ({sw.ElapsedMilliseconds} ms)");
    }
}

Console.WriteLine();
if (hadFailure)
{
    Console.WriteLine("❌ 连通性验证失败：至少一只股票行情未取到。");
    return 1;
}

Console.WriteLine("✅ 连通性验证通过：A股与港股行情均已取到。");
return 0;
