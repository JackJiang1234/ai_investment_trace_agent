using System.Text;
using Agent.Core;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Agent.Console.Tests;

/// <summary>验证 appsettings 中 v1.1 新增配置项能正确绑定到强类型 Options。</summary>
public class ConfigBindingTests
{
    private const string Json = """
    {
      "Agent": {
        "Stocks": [
          { "Code": "600519.SH", "Group": "核心持仓", "Shares": 100, "CostPrice": 1450.0, "IdealBuyMarketCapYi": 18000, "SellMarketCapYi": 28000 },
          { "Code": "00700.HK", "Group": "观察池", "IdealBuyMarketCapYi": 27000 },
          { "Code": "515170.SH", "Group": "核心持仓", "SecurityType": "Etf", "Shares": 100000, "CostPrice": 0.56, "IdealBuyPrice": 0.42, "SellPrice": 0.70 }
        ],
        "Announcements": { "LookbackDays": 7 },
        "Currency": { "BaseCurrency": "CNY", "StaticRates": { "HKD": 0.87 } },
        "Report": { "Formats": [ "Html", "Markdown", "WeChatHtml" ] }
      }
    }
    """;

    private static AgentOptions Bind()
    {
        var config = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(Json)))
            .Build();
        return config.GetSection(AgentOptions.SectionName).Get<AgentOptions>()!;
    }

    [Fact]
    public void Binds_CoreHolding_Fields()
    {
        var core = Bind().Stocks[0];

        core.Shares.Should().Be(100);
        core.CostPrice.Should().Be(1450.0m);
        core.IdealBuyMarketCapYi.Should().Be(18000m);
        core.SellMarketCapYi.Should().Be(28000m);
    }

    [Fact]
    public void Binds_WatchlistStock_WithNullableHoldingsAbsent()
    {
        var watch = Bind().Stocks[1];

        watch.Shares.Should().BeNull();
        watch.CostPrice.Should().BeNull();
        watch.IdealBuyMarketCapYi.Should().Be(27000m); // 观察池也可配买点
        watch.SellMarketCapYi.Should().BeNull();
    }

    [Fact]
    public void Binds_AnnouncementLookbackDays()
    {
        Bind().Announcements.LookbackDays.Should().Be(7);
    }

    [Fact]
    public void Binds_Currency()
    {
        var currency = Bind().Currency;

        currency.BaseCurrency.Should().Be("CNY");
        currency.StaticRates["HKD"].Should().Be(0.87m);
    }

    [Fact]
    public void Binds_WeChatHtmlFormat()
    {
        Bind().Report.Formats.Should().Contain(ReportFormat.WeChatHtml);
    }

    [Fact]
    public void Binds_EtfSecurityTypeAndPriceThresholds()
    {
        var etf = Bind().Stocks[2];

        etf.SecurityType.Should().Be(SecurityType.Etf);
        etf.IdealBuyPrice.Should().Be(0.42m);
        etf.SellPrice.Should().Be(0.70m);
        etf.IdealBuyMarketCapYi.Should().BeNull();
    }
}
