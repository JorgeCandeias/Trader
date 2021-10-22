using Outcompute.Trader.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public interface ITickerProvider
    {
        public Task SetTickerAsync(MiniTicker ticker, CancellationToken cancellationToken = default);

        Task<MiniTicker?> TryGetTickerAsync(string symbol, CancellationToken cancellationToken = default);
    }
}