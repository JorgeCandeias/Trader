using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    public interface IKlineSynchronizer
    {
        Task SyncAsync(IEnumerable<(string Symbol, KlineInterval Interval, int Periods)> windows, CancellationToken cancellationToken);
    }
}