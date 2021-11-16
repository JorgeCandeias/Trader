using Orleans;
using Outcompute.Trader.Trading.Readyness;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData;

internal interface IBinanceMarketDataGrain : IGrainWithGuidKey
{
    /// <inheritdoc cref="IReadynessProvider.IsReadyAsync(System.Threading.CancellationToken)"/>
    Task<bool> IsReadyAsync();

    Task PingAsync();
}