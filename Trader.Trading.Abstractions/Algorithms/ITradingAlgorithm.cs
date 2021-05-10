using System.Threading;
using System.Threading.Tasks;
using Trader.Models;

namespace Trader.Trading.Algorithms
{
    public interface ITradingAlgorithm
    {
        string Symbol { get; }

        Task InitializeAsync(ExchangeInfo exchangeInfo, CancellationToken cancellationToken = default);

        Task GoAsync(CancellationToken cancellationToken = default);

        Task<Profit> GetProfitAsync(CancellationToken cancellationToken = default);

        Task<Statistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
    }
}