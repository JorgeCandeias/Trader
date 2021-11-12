using Orleans;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Klines
{
    public interface IKlineProviderReplicaGrain : IGrainWithStringKey
    {
        ValueTask<IReadOnlyCollection<Kline>> GetKlinesAsync();

        ValueTask<IReadOnlyCollection<Kline>> GetKlinesAsync(DateTime tickTime, int periods);

        ValueTask<Kline?> TryGetKlineAsync(DateTime openTime);

        ValueTask SetKlineAsync(Kline item);

        ValueTask SetKlinesAsync(IEnumerable<Kline> items);

        ValueTask<DateTime?> TryGetLastOpenTimeAsync();
    }
}