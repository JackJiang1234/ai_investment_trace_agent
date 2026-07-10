using Agent.DataSource;
using FluentAssertions;

namespace Agent.DataSource.Tests;

public class ExchangeRateParserTests
{
    private static string ReadFixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public void Parse_HkdCnyFixture_ScalesByFourDecimals()
    {
        var rate = ExchangeRateParser.Parse(ReadFixture("eastmoney_fx_hkdcny.json"));

        rate.Should().Be(0.8649m); // f43 8649 / 10^4
    }

    [Fact]
    public void Parse_EmptyOrGarbage_ReturnsNull()
    {
        ExchangeRateParser.Parse("").Should().BeNull();
        ExchangeRateParser.Parse("not json").Should().BeNull();
        ExchangeRateParser.Parse("{\"data\":null}").Should().BeNull();
    }

    [Fact]
    public void Parse_NonPositiveRate_ReturnsNull()
    {
        ExchangeRateParser.Parse("{\"data\":{\"f43\":0,\"f59\":4}}").Should().BeNull();
    }
}
