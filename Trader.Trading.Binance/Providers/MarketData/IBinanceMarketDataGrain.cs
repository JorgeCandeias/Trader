using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using Outcompute.Trader.Trading.Readyness;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    internal interface IBinanceMarketDataGrain : IGrainWithGuidKey
    {
        /// <inheritdoc cref="IReadynessProvider.IsReadyAsync(System.Threading.CancellationToken)"/>
        Task<bool> IsReadyAsync();

        /// <inheritdoc cref="ITickerProvider.TryGetTickerAsync(string, System.Threading.CancellationToken)"/>
        ValueTask<MiniTicker?> TryGetTickerAsync(string symbol);
    }
}