using Orleans;
using Outcompute.Trader.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Tickers
{
    internal class TickerProvider : ITickerProvider
    {
        private readonly IGrainFactory _factory;

        public TickerProvider(IGrainFactory factory)
        {
            _factory = factory;
        }

        public Task SetTickerAsync(MiniTicker ticker, CancellationToken cancellationToken = default)
        {
            if (ticker is null) throw new ArgumentNullException(nameof(ticker));

            return _factory.GetTickerProviderReplicaGrain(ticker.Symbol).SetTickerAsync(ticker);
        }

        public Task<MiniTicker?> TryGetTickerAsync(string symbol, CancellationToken cancellationToken = default)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return _factory.GetTickerProviderReplicaGrain(symbol).TryGetTickerAsync();
        }
    }
}