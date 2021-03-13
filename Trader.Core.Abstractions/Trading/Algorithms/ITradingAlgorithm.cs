using System.Collections.Generic;

namespace Trader.Core.Trading.Algorithms
{
    public interface ITradingAlgorithm
    {
        string Symbol { get; }

        IEnumerable<AccountTrade> GetTrades();
    }
}