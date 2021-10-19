using Orleans;
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

        public KlineProvider(IGrainFactory factory)
        {
            _factory = factory;
        }

        public Task<IReadOnlyList<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, CancellationToken cancellationToken = default)
        {
            return _factory.GetKlineProviderReplicaGrain(symbol, interval).GetKlinesAsync();
        }

        public Task<Kline?> TryGetKlineAsync(string symbol, KlineInterval interval, DateTime openTime, CancellationToken cancellationToken = default)
        {
            return _factory.GetKlineProviderReplicaGrain(symbol, interval).TryGetKlineAsync(openTime);
        }
    }
}