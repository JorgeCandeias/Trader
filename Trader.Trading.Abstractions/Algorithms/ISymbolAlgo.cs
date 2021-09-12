using Outcompute.Trader.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public interface ISymbolAlgo : IAlgo
    {
        string Symbol { get; }

        Task InitializeAsync(ExchangeInfo exchangeInfo, CancellationToken cancellationToken = default);

        Task<Profit> GetProfitAsync(CancellationToken cancellationToken = default);

        Task<Statistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
    }
}