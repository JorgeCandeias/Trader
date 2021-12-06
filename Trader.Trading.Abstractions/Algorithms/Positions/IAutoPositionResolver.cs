namespace Outcompute.Trader.Trading.Algorithms.Positions;

public interface IAutoPositionResolver
{
    AutoPosition Resolve(Symbol symbol, ImmutableSortedSet<OrderQueryResult> orders, ImmutableSortedSet<AccountTrade> trades, DateTime startTime);
}