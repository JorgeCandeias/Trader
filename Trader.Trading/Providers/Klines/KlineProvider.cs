namespace Outcompute.Trader.Trading.Providers.Klines;

internal class KlineProvider : IKlineProvider
{
    private readonly IGrainFactory _factory;

    public KlineProvider(IGrainFactory factory)
    {
        _factory = factory;
    }

    public Task SetLastSyncedKlineOpenTimeAsync(string symbol, KlineInterval interval, DateTime time, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return _factory.GetKlineProviderGrain(symbol, interval).SetLastSyncedKlineOpenTimeAsync(time);
    }

    public Task<DateTime> GetLastSyncedKlineOpenTimeAsync(string symbol, KlineInterval interval, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return _factory.GetKlineProviderGrain(symbol, interval).GetLastSyncedKlineOpenTimeAsync();
    }

    public ValueTask<ImmutableSortedSet<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return _factory.GetKlineProviderReplicaGrain(symbol, interval).GetKlinesAsync();
    }

    public ValueTask<ImmutableSortedSet<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime tickTime, int periods, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return _factory.GetKlineProviderReplicaGrain(symbol, interval).GetKlinesAsync(tickTime, periods);
    }

    public ValueTask SetKlineAsync(Kline item, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(item, nameof(item));

        return _factory.GetKlineProviderReplicaGrain(item.Symbol, item.Interval).SetKlineAsync(item);
    }

    public ValueTask<Kline?> TryGetKlineAsync(string symbol, KlineInterval interval, DateTime openTime, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return _factory.GetKlineProviderReplicaGrain(symbol, interval).TryGetKlineAsync(openTime);
    }

    public ValueTask SetKlinesAsync(string symbol, KlineInterval interval, IEnumerable<Kline> items, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(symbol, nameof(symbol));
        Guard.IsNotNull(items, nameof(items));

        return _factory.GetKlineProviderReplicaGrain(symbol, interval).SetKlinesAsync(items);
    }

    public ValueTask ConflateKlineAsync(Kline item, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(item, nameof(item));

        return _factory.GetKlineConflaterGrain(item.Symbol, item.Interval).PushAsync(item);
    }

    public ValueTask<DateTime?> TryGetLastOpenTimeAsync(string symbol, KlineInterval interval, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return _factory.GetKlineProviderReplicaGrain(symbol, interval).TryGetLastOpenTimeAsync();
    }
}