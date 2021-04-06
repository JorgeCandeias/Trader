using System.Threading;
using System.Threading.Tasks;
using Trader.Data;

namespace Trader.Trading.Algorithms
{
    public interface ITradingAlgorithm
    {
        string Symbol { get; }

        Task<Profit> GoAsync(ExchangeInfo exchangeInfo, AccountInfo accountInfo, CancellationToken cancellationToken = default);
    }
}