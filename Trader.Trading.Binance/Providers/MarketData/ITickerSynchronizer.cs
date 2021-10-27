using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    internal interface ITickerSynchronizer
    {
        Task SyncAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default);
    }
}