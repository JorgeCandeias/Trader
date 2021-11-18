using Orleans.Concurrency;

namespace Outcompute.Trader.Trading.Algorithms.Positions;

[Immutable]
public record Position(string Symbol, long OrderId, decimal Price, decimal Quantity, DateTime Time)
{
    public static Position Empty { get; } = new Position(string.Empty, 0, 0, 0, DateTime.MinValue);
}