using Agent.Core;
using FluentAssertions;

namespace Agent.Core.Tests;

public class HoldingCalculatorTests
{
    private static Quote Quote(string code, decimal price, decimal totalMarketCap) => new()
    {
        Code = StockCode.Parse(code),
        Name = code,
        Price = price,
        PreviousClose = price,
        Open = price,
        High = price,
        Low = price,
        ChangeAmount = 0m,
        ChangePercent = 0m,
        Volume = 0,
        Turnover = 0m,
        TurnoverRate = 0m,
        VolumeRatio = 0m,
        TotalMarketCap = totalMarketCap,
    };

    // 汇率对 A 股无影响；此处给港股用。
    private static readonly CurrencyConverter Rate09 = new(0.9m, isLiveRate: true);

    [Fact]
    public void AShare_Holding_ComputesCnyMetricsAndHitsBuyPoint()
    {
        var quote = Quote("600519.SH", price: 1200m, totalMarketCap: 1_500_000_000_000m); // 1.5万亿元
        var config = new StockConfig
        {
            Code = "600519.SH",
            Group = "核心持仓",
            Shares = 100,
            CostPrice = 1450m,
            IdealBuyMarketCapYi = 18000m,
            SellMarketCapYi = 28000m,
        };

        var m = HoldingCalculator.Compute(quote, config, Rate09, yearStartClose: 1600m);

        m.Currency.Should().Be(Currency.CNY);
        m.TotalMarketCapCnyYi.Should().Be(15000m);       // 1.5e12 / 1e8
        m.HoldingValueCny.Should().Be(120000m);           // 1200 * 100
        m.YearStartValueCny.Should().Be(160000m);         // 1600 * 100
        m.CostValueCny.Should().Be(145000m);              // 1450 * 100
        m.CostProfitCny.Should().Be(-25000m);             // (1200-1450) * 100
        m.CostReturnPercent.Should().BeApproximately(-17.24m, 0.01m); // (1200-1450)/1450
        m.YtdReturnPercent.Should().Be(-25m);             // (1200-1600)/1600
        m.CanBuy.Should().BeTrue();                       // 15000 < 18000 → 击球
        m.ShouldSell.Should().BeFalse();                  // 15000 > 28000 假
        m.Shares.Should().Be(100);
        m.CostPrice.Should().Be(1450m);
        m.IdealBuyYi.Should().Be(18000m);
        m.SellYi.Should().Be(28000m);
    }

    [Fact]
    public void HkShare_FoldsToCny_ForMarketCapAndHolding()
    {
        var quote = Quote("00700.HK", price: 300m, totalMarketCap: 3_000_000_000_000m); // 3万亿港元
        var config = new StockConfig
        {
            Code = "00700.HK",
            Group = "核心持仓",
            Shares = 200,
            CostPrice = 250m,
            IdealBuyMarketCapYi = 30000m,
            SellMarketCapYi = 55000m,
        };

        var m = HoldingCalculator.Compute(quote, config, Rate09, yearStartClose: 250m);

        m.Currency.Should().Be(Currency.HKD);
        m.TotalMarketCapCnyYi.Should().Be(27000m);   // 3e12 * 0.9 / 1e8
        m.HoldingValueCny.Should().Be(54000m);        // 300 * 200 * 0.9
        m.YearStartValueCny.Should().Be(45000m);      // 250 * 200 * 0.9
        m.CostValueCny.Should().Be(45000m);           // 250 * 200 * 0.9
        m.CostProfitCny.Should().Be(9000m);           // (300-250) * 200 * 0.9
        m.CostReturnPercent.Should().Be(20m);         // (300-250)/250, 原币种不受汇率影响
        m.YtdReturnPercent.Should().Be(20m);          // (300-250)/250, 币种自身不折
        m.CanBuy.Should().BeTrue();                   // 27000 < 30000 → 击球
        m.ShouldSell.Should().BeFalse();
    }

    [Fact]
    public void Watchlist_NoShares_StillEvaluatesBuyPoint()
    {
        var quote = Quote("00388.HK", price: 250m, totalMarketCap: 200_000_000_000m); // 2000亿港元
        var config = new StockConfig
        {
            Code = "00388.HK",
            Group = "观察池",
            IdealBuyMarketCapYi = 3000m,
        };

        var m = HoldingCalculator.Compute(quote, config, Rate09, yearStartClose: null);

        m.HoldingValueCny.Should().BeNull();          // 无持股
        m.YearStartValueCny.Should().BeNull();        // 无持股 → 年初持仓市值也为空
        m.CostValueCny.Should().BeNull();             // 无成本价
        m.CostProfitCny.Should().BeNull();
        m.CostReturnPercent.Should().BeNull();
        m.Shares.Should().BeNull();
        m.TotalMarketCapCnyYi.Should().Be(1800m);     // 2e11 * 0.9 / 1e8
        m.CanBuy.Should().BeTrue();                    // 1800 < 3000 → 击球
        m.ShouldSell.Should().BeFalse();               // 未配卖点
        m.YtdReturnPercent.Should().BeNull();          // 无年初收盘
    }

    [Fact]
    public void NoBuyPointConfigured_NeverHitsBuy()
    {
        var quote = Quote("600519.SH", price: 1200m, totalMarketCap: 100_000_000_000m); // 极便宜 1000亿
        var config = new StockConfig { Code = "600519.SH", IdealBuyMarketCapYi = null };

        var m = HoldingCalculator.Compute(quote, config, Rate09, yearStartClose: null);

        m.CanBuy.Should().BeFalse();
    }

    [Fact]
    public void MissingMarketCap_YieldsNullYi_AndNoSignals()
    {
        var quote = Quote("600519.SH", price: 1200m, totalMarketCap: 0m);
        var config = new StockConfig
        {
            Code = "600519.SH",
            IdealBuyMarketCapYi = 18000m,
            SellMarketCapYi = 28000m,
        };

        var m = HoldingCalculator.Compute(quote, config, Rate09, yearStartClose: 1600m);

        m.TotalMarketCapCnyYi.Should().BeNull();
        m.CanBuy.Should().BeFalse();
        m.ShouldSell.Should().BeFalse();
    }

    [Fact]
    public void SellPoint_HitWhenMarketCapAboveThreshold()
    {
        var quote = Quote("600519.SH", price: 1200m, totalMarketCap: 3_000_000_000_000m); // 3万亿 = 30000亿
        var config = new StockConfig
        {
            Code = "600519.SH",
            IdealBuyMarketCapYi = 18000m,
            SellMarketCapYi = 28000m,
        };

        var m = HoldingCalculator.Compute(quote, config, Rate09, yearStartClose: 1600m);

        m.CanBuy.Should().BeFalse();     // 30000 < 18000 假
        m.ShouldSell.Should().BeTrue();  // 30000 > 28000 → 卖点
    }

    [Fact]
    public void Etf_ExplicitType_UsesPriceThresholds_NotMarketCap()
    {
        // ETF f116=基金规模 40亿; 若按市值口径 40<700 会误判击球。显式 ETF → 按价格口径。
        var quote = Quote("515170.SH", price: 0.439m, totalMarketCap: 4_000_000_000m);
        var config = new StockConfig
        {
            Code = "515170.SH",
            SecurityType = SecurityType.Etf,
            Shares = 309400,
            CostPrice = 0.56m,
            IdealBuyPrice = 0.50m,
            SellPrice = 0.75m,
        };

        var m = HoldingCalculator.Compute(quote, config, Rate09, yearStartClose: 0.557m);

        m.IsEtf.Should().BeTrue();
        m.TotalMarketCapCnyYi.Should().Be(40m);   // 基金规模，仅展示用
        m.CanBuy.Should().BeTrue();                // 0.439 < 0.50 理想买入价
        m.ShouldSell.Should().BeFalse();           // 0.439 > 0.75 假
        m.IdealBuyPrice.Should().Be(0.50m);
        m.HoldingValueCny.Should().Be(0.439m * 309400);
    }

    [Fact]
    public void Etf_AutoDetectedByCodePrefix_WhenTypeOmitted()
    {
        var quote = Quote("515170.SH", price: 0.80m, totalMarketCap: 4_000_000_000m);
        var config = new StockConfig
        {
            Code = "515170.SH",           // 未配 SecurityType → 按前缀识别为 ETF
            IdealBuyPrice = 0.50m,
            SellPrice = 0.75m,
        };

        var m = HoldingCalculator.Compute(quote, config, Rate09, yearStartClose: null);

        m.IsEtf.Should().BeTrue();
        m.CanBuy.Should().BeFalse();   // 0.80 < 0.50 假
        m.ShouldSell.Should().BeTrue(); // 0.80 > 0.75 卖点
    }

    [Fact]
    public void Etf_MarketCapThresholdsIgnored_EvenIfConfigured()
    {
        // 即使误配了市值阈值，ETF 也不按市值判定（避免"规模<阈值恒击球"）。
        var quote = Quote("515170.SH", price: 0.80m, totalMarketCap: 4_000_000_000m);
        var config = new StockConfig
        {
            Code = "515170.SH",
            SecurityType = SecurityType.Etf,
            IdealBuyMarketCapYi = 700m,   // 40 < 700 但不应触发
        };

        var m = HoldingCalculator.Compute(quote, config, Rate09, yearStartClose: null);

        m.CanBuy.Should().BeFalse();
    }

    [Fact]
    public void ZeroYearStartClose_YieldsNullYtd()
    {
        var quote = Quote("600519.SH", price: 1200m, totalMarketCap: 1_500_000_000_000m);
        var config = new StockConfig { Code = "600519.SH" };

        var m = HoldingCalculator.Compute(quote, config, Rate09, yearStartClose: 0m);

        m.YtdReturnPercent.Should().BeNull();
    }
}
