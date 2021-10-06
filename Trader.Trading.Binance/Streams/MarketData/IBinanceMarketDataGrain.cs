using Orleans;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Streams.MarketData
{
    internal interface IBinanceMarketDataGrain : IGrainWithGuidKey
    {
        Task<MiniTicker?> TryGetTickerAsync(string symbol);

        Task<IEnumerable<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime start, DateTime end);
    }
}