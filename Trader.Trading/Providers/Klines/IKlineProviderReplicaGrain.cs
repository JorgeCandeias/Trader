using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;

namespace Outcompute.Trader.Trading.Providers.Klines;

public interface IKlineProviderReplicaGrain : IGrainWithStringKey
{
    ValueTask<KlineCollection> GetKlinesAsync();

    ValueTask<KlineCollection> GetKlinesAsync(DateTime tickTime, int periods);

    ValueTask<Kline?> TryGetKlineAsync(DateTime openTime);

    ValueTask SetKlineAsync(Kline item);

    ValueTask SetKlinesAsync(IEnumerable<Kline> items);

    ValueTask<DateTime?> TryGetLastOpenTimeAsync();
}