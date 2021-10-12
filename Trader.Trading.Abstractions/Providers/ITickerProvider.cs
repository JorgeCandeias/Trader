using Outcompute.Trader.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public interface ITickerProvider
    {
        /// <summary>
        /// Gets the ticker for the specified symbol if it exists, otherwise null.
        /// </summary>
        ValueTask<MiniTicker?> TryGetTickerAsync(string symbol, CancellationToken cancellationToken = default);
    }
}