using Orleans;
using Orleans.Concurrency;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Readyness;
using System;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    internal interface IBinanceMarketDataGrain : IGrainWithGuidKey
    {
        /// <inheritdoc cref="IReadynessProvider.IsReadyAsync(System.Threading.CancellationToken)"/>
        Task<bool> IsReadyAsync();

        /// <summary>
        /// Long polls for a new ticker for the specified symbol.
        /// Return a null ticker and empty version if the poll cannot resolve within the configured reactive polling delay.
        /// </summary>
        [AlwaysInterleave]
        ValueTask<(MiniTicker?, Guid)> LongPollTickerAsync(string symbol, Guid version);
    }
}