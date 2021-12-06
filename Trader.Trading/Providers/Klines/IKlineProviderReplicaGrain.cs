namespace Outcompute.Trader.Trading.Providers.Klines;

public interface IKlineProviderReplicaGrain : IGrainWithStringKey
{
    ValueTask<ImmutableSortedSet<Kline>> GetKlinesAsync();

    ValueTask<ImmutableSortedSet<Kline>> GetKlinesAsync(DateTime tickTime, int periods);

    ValueTask<Kline?> TryGetKlineAsync(DateTime openTime);

    ValueTask SetKlineAsync(Kline item);

    ValueTask SetKlinesAsync(IEnumerable<Kline> items);

    ValueTask<DateTime?> TryGetLastOpenTimeAsync();
}