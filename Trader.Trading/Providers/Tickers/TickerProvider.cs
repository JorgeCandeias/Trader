using Orleans;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Tickers
{
    internal class TickerProvider : ITickerProvider
    {
        private readonly IGrainFactory _factory;
        private readonly ITradingRepository _repository;

        public TickerProvider(IGrainFactory factory, ITradingRepository repository)
        {
            _factory = factory;
            _repository = repository;
        }

        public Task SetTickerAsync(MiniTicker ticker, CancellationToken cancellationToken = default)
        {
            if (ticker is null) throw new ArgumentNullException(nameof(ticker));

            return SetTickerCoreAsync(ticker, cancellationToken);
        }

        private async Task SetTickerCoreAsync(MiniTicker ticker, CancellationToken cancellationToken)
        {
            await _repository.SetTickerAsync(ticker, cancellationToken);

            await _factory.GetTickerProviderReplicaGrain(ticker.Symbol).SetTickerAsync(ticker);
        }

        public Task<MiniTicker?> TryGetTickerAsync(string symbol, CancellationToken cancellationToken = default)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return _factory.GetTickerProviderReplicaGrain(symbol).TryGetTickerAsync();
        }
    }
}