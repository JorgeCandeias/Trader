using Orleans;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Klines
{
    internal class KlineProvider : IKlineProvider
    {
        private readonly IGrainFactory _factory;
        private readonly ITradingRepository _repository;

        public KlineProvider(IGrainFactory factory, ITradingRepository repository)
        {
            _factory = factory;
            _repository = repository;
        }

        public Task<IReadOnlyList<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, CancellationToken cancellationToken = default)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return _factory.GetKlineProviderReplicaGrain(symbol, interval).GetKlinesAsync();
        }

        public Task SetKlineAsync(Kline item, CancellationToken cancellationToken = default)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));

            return SetKlineCoreAsync(item, cancellationToken);
        }

        private async Task SetKlineCoreAsync(Kline item, CancellationToken cancellationToken = default)
        {
            await _repository.SetKlineAsync(item, cancellationToken).ConfigureAwait(false);

            await _factory.GetKlineProviderReplicaGrain(item.Symbol, item.Interval).SetKlineAsync(item).ConfigureAwait(false);
        }

        public Task<Kline?> TryGetKlineAsync(string symbol, KlineInterval interval, DateTime openTime, CancellationToken cancellationToken = default)
        {
            return _factory.GetKlineProviderReplicaGrain(symbol, interval).TryGetKlineAsync(openTime);
        }

        public Task SetKlinesAsync(string symbol, KlineInterval interval, IEnumerable<Kline> items, CancellationToken cancellationToken = default)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));
            if (items is null) throw new ArgumentNullException(nameof(items));

            foreach (var item in items)
            {
                if (item.Symbol != symbol) throw new ArgumentOutOfRangeException(nameof(items), $"Kline has symbol '{item.Symbol}' different from partition symbol '{symbol}'");
                if (item.Interval != interval) throw new ArgumentOutOfRangeException(nameof(items), $"Kline has interval '{item.Interval}' different from partition interval '{interval}'");
            }

            return SetKlinesCoreAsync(symbol, interval, items, cancellationToken);
        }

        private async Task SetKlinesCoreAsync(string symbol, KlineInterval interval, IEnumerable<Kline> items, CancellationToken cancellationToken = default)
        {
            await _repository.SetKlinesAsync(items, cancellationToken).ConfigureAwait(false);

            await _factory.GetKlineProviderReplicaGrain(symbol, interval).SetKlinesAsync(items).ConfigureAwait(false);
        }

        public Task<DateTime?> TryGetLastOpenTimeAsync(string symbol, KlineInterval interval, CancellationToken cancellationToken = default)
        {
            return _factory.GetKlineProviderReplicaGrain(symbol, interval).TryGetLastOpenTimeAsync();
        }
    }
}