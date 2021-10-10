using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    internal class BinanceKlineProvider : IKlineProvider
    {
        private readonly IBinanceMarketDataGrain _grain;

        public BinanceKlineProvider(IGrainFactory factory)
        {
            _ = factory ?? throw new ArgumentNullException(nameof(factory));

            _grain = factory.GetBinanceMarketDataGrain();
        }

        public Task<IReadOnlyList<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime start, DateTime end, CancellationToken cancellationToken = default)
        {
            return _grain.GetKlinesAsync(symbol, interval, start, end);
        }
    }
}