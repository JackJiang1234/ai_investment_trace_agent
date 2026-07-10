namespace Agent.Core;

/// <summary>货币种类（本期仅人民币与港元）。</summary>
public enum Currency
{
    /// <summary>人民币（基准货币）。</summary>
    CNY,

    /// <summary>港元。</summary>
    HKD,
}

/// <summary>市场与货币的映射。</summary>
public static class CurrencyExtensions
{
    /// <summary>按市场推断计价货币：港股为港元，A股（沪/深）为人民币。</summary>
    public static Currency ToCurrency(this Market market) =>
        market == Market.HK ? Currency.HKD : Currency.CNY;
}
