using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData;

public interface IKlineSynchronizer
{
    Task SyncAsync(IEnumerable<(string Symbol, KlineInterval Interval, int Periods)> windows, CancellationToken cancellationToken);
}