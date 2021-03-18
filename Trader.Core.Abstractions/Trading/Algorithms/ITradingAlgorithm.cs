using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Trader.Core.Trading.Algorithms
{
    public interface ITradingAlgorithm
    {
        string Symbol { get; }

        Task<ImmutableList<AccountTrade>> GetTradesAsync();

        Task GoAsync(ExchangeInfo exchangeInfo, AccountInfo accountInfo, CancellationToken cancellationToken = default);
    }
}