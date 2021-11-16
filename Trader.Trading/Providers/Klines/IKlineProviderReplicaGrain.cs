using Orleans;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers.Klines;

public interface IKlineProviderReplicaGrain : IGrainWithStringKey
{
    ValueTask<IReadOnlyList<Kline>> GetKlinesAsync();

    ValueTask<IReadOnlyList<Kline>> GetKlinesAsync(DateTime tickTime, int periods);

    ValueTask<Kline?> TryGetKlineAsync(DateTime openTime);

    ValueTask SetKlineAsync(Kline item);

    ValueTask SetKlinesAsync(IEnumerable<Kline> items);

    ValueTask<DateTime?> TryGetLastOpenTimeAsync();
}