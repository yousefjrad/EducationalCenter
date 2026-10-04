using EducationalCenter.Domain.Enums;

namespace EducationalCenter.Application.Common.Money;

internal static class CurrencyConversion
{
    /// <summary>
    /// SYP equivalent of an amount. USD uses the exchange rate (SYP per 1 USD) entered by the staff member,
    /// rounded to whole SYP. The caller must have validated that a rate exists for USD.
    /// </summary>
    public static decimal ToSyp(Currency currency, decimal amount, decimal? exchangeRate) =>
        currency == Currency.Usd
            ? Math.Round(amount * exchangeRate!.Value, 0, MidpointRounding.AwayFromZero)
            : amount;
}
