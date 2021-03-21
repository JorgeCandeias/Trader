using Trader.Data;

namespace Trader.Trading.Pnl
{
    public interface IProfitCalculator
    {
        Profit Calculate(SortedTradeSet trades);
    }
}