using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    internal interface IBinanceMarketDataGrain : IGrainWithGuidKey
    {
        Task<MiniTicker?> TryGetTickerAsync(string symbol);

        /// <inheritdoc cref="IKlineProvider.GetKlinesAsync(string, KlineInterval, DateTime, DateTime, System.Threading.CancellationToken)"/>
        Task<IReadOnlyList<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime start, DateTime end);

        Task<bool> IsReadyAsync();
    }
}