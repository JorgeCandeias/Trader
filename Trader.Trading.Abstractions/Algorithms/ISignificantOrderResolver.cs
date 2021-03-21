using Trader.Data;

namespace Trader.Trading.Algorithms
{
    public interface ISignificantOrderResolver
    {
        SortedOrderSet Resolve(SortedOrderSet orders, SortedTradeSet trades);
    }
}