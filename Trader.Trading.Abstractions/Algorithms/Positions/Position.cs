using Orleans.Concurrency;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Algorithms.Positions;

[Immutable]
public record Position(Symbol Symbol, long OrderId, decimal Price, decimal Quantity, DateTime Time)
{
    public static Position Empty { get; } = new Position(Symbol.Empty, 0, 0, 0, DateTime.MinValue);
}