using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    internal class BinanceTickerProvider : ITickerProvider
    {
        private readonly IGrainFactory _factory;

        public BinanceTickerProvider(IGrainFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public ValueTask<MiniTicker?> TryGetTickerAsync(string symbol, CancellationToken cancellationToken = default)
        {
            // redirect the call to the binance market data grain
            return _factory.GetBinanceTickerProviderGrain(symbol).TryGetTickerAsync();
        }
    }
}