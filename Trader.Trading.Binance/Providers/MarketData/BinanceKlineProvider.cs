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
        private readonly IGrainFactory _factory;

        public BinanceKlineProvider(IGrainFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public ValueTask<IReadOnlyCollection<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime start, DateTime end, CancellationToken cancellationToken = default)
        {
            return _factory.GetBinanceKlineProviderGrain(symbol, interval).GetKlinesAsync(start, end);
        }
    }
}