namespace Trader.Core.Trading.Algorithms
{
    public interface ISignificantOrderResolver
    {
        SortedOrderSet Resolve(SortedOrderSet orders, SortedTradeSet trades);
    }
}