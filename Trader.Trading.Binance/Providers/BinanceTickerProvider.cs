using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Binance.Streams.MarketData;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers
{
    internal class BinanceTickerProvider : ITickerProvider
    {
        private readonly IBinanceMarketDataGrain _grain;

        public BinanceTickerProvider(IGrainFactory factory)
        {
            _ = factory ?? throw new ArgumentNullException(nameof(factory));

            _grain = factory.GetBinanceMarketDataGrain();
        }

        public Task<MiniTicker?> TryGetTickerAsync(string symbol, CancellationToken cancellationToken = default)
        {
            // redirect the call to the binance market data grain
            return _grain.TryGetTickerAsync(symbol);
        }
    }
}