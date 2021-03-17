using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Trader.Core.Trading.Algorithms
{
    public interface ITradingAlgorithm
    {
        string Symbol { get; }

        IEnumerable<AccountTrade> GetTrades();

        Task GoAsync(ExchangeInfo exchangeInfo, AccountInfo accountInfo, CancellationToken cancellationToken = default);
    }
}