using Agent.Core;
using FluentAssertions;

namespace Agent.Core.Tests;

public class CurrencyConverterTests
{
    [Fact]
    public void ToCny_Cny_IsPassthrough()
    {
        var converter = new CurrencyConverter(0.8649m, isLiveRate: true);

        converter.ToCny(100m, Currency.CNY).Should().Be(100m);
    }

    [Fact]
    public void ToCny_Hkd_MultipliesByRate()
    {
        var converter = new CurrencyConverter(0.8649m, isLiveRate: true);

        converter.ToCny(100m, Currency.HKD).Should().Be(86.49m);
    }

    [Fact]
    public void Ctor_NonPositiveRate_Throws()
    {
        var act = () => new CurrencyConverter(0m, isLiveRate: false);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ExposesRateAndLiveFlag()
    {
        var converter = new CurrencyConverter(0.9m, isLiveRate: false);

        converter.HkdToCny.Should().Be(0.9m);
        converter.IsLiveRate.Should().BeFalse();
    }

    [Theory]
    [InlineData(Market.SH, Currency.CNY)]
    [InlineData(Market.SZ, Currency.CNY)]
    [InlineData(Market.HK, Currency.HKD)]
    public void Market_MapsToCurrency(Market market, Currency expected)
    {
        market.ToCurrency().Should().Be(expected);
    }
}
