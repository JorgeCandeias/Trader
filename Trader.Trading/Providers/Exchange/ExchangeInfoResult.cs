using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers.Exchange;

/// <summary>
/// Model for <see cref="ExchangeInfo"/> get requests.
/// </summary>
internal readonly struct ExchangeInfoResult
{
    public ExchangeInfoResult(ExchangeInfo value, Guid version)
    {
        Value = value;
        Version = version;
    }

    public ExchangeInfo Value { get; }
    public Guid Version { get; }

    public void Deconstruct(out ExchangeInfo value, out Guid version)
    {
        value = Value;
        version = Version;
    }
}