using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers.Trades;

internal readonly struct ReactiveResult
{
    public ReactiveResult(Guid version, int serial, IReadOnlyList<AccountTrade> trades)
    {
        Version = version;
        Serial = serial;
        Trades = trades;
    }

    public Guid Version { get; }
    public int Serial { get; }
    public IReadOnlyList<AccountTrade> Trades { get; }
}