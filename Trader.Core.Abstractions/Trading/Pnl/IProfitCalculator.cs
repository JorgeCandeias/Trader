using System.Collections.Generic;

namespace Trader.Core.Trading.ProfitCalculation
{
    public interface IProfitCalculator
    {
        Profit Calculate(IEnumerable<AccountTrade> trades);
    }
}