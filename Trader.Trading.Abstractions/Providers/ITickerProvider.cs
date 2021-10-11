using Outcompute.Trader.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public interface ITickerProvider
    {
        ValueTask<MiniTicker?> TryGetTickerAsync(string symbol, CancellationToken cancellationToken = default);
    }
}