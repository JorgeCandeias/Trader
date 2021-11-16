using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers.Exchange;

/// <summary>
/// Model for versioned <see cref="ExchangeInfo"/> try requests.
/// </summary>
internal readonly struct ExchangeInfoTryResult
{
    public ExchangeInfoTryResult(ExchangeInfo? value, Guid version)
    {
        Value = value;
        Version = version;
    }

    public ExchangeInfo? Value { get; }
    public Guid Version { get; }
}