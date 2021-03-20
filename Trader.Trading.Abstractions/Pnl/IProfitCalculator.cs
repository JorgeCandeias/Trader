using System.Collections.Generic;
using Trader.Data;

namespace Trader.Trading.Pnl
{
    public interface IProfitCalculator
    {
        Profit Calculate(IEnumerable<AccountTrade> trades);
    }
}