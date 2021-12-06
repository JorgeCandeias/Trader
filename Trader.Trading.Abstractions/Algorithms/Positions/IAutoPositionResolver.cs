namespace Outcompute.Trader.Trading.Algorithms.Positions;

public interface IAutoPositionResolver
{
    AutoPosition Resolve(Symbol symbol, ImmutableSortedSet<OrderQueryResult> orders, TradeCollection trades, DateTime startTime);
}