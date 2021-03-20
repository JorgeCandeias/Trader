using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Trader.Data;

namespace Trader.Trading.Algorithms
{
    public interface ITradingAlgorithm
    {
        string Symbol { get; }

        ValueTask<ImmutableList<AccountTrade>> GetTradesAsync(CancellationToken cancellationToken = default);

        Task GoAsync(ExchangeInfo exchangeInfo, AccountInfo accountInfo, CancellationToken cancellationToken = default);
    }
}