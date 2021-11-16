using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers.Orders;

internal readonly struct ReactiveResult
{
    public ReactiveResult(Guid version, int serial, IReadOnlyList<OrderQueryResult> items)
    {
        Version = version;
        Serial = serial;
        Items = items;
    }

    public Guid Version { get; }
    public int Serial { get; }
    public IReadOnlyList<OrderQueryResult> Items { get; }
}