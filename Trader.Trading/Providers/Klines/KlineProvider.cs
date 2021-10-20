using Orleans;
using Orleans.Runtime;
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
        private readonly ILocalSiloDetails _details;

        public KlineProvider(IGrainFactory factory, ILocalSiloDetails details)
        {
            _factory = factory;
            _details = details;
        }

        public Task<IReadOnlyList<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, CancellationToken cancellationToken = default)
        {
            return _factory.GetKlineProviderReplicaGrain(_details, symbol, interval).GetKlinesAsync();
        }

        public Task<Kline?> TryGetKlineAsync(string symbol, KlineInterval interval, DateTime openTime, CancellationToken cancellationToken = default)
        {
            return _factory.GetKlineProviderReplicaGrain(_details, symbol, interval).TryGetKlineAsync(openTime);
        }
    }
}