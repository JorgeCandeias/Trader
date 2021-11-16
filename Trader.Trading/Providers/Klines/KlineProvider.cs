using Orleans;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers.Klines;

internal class KlineProvider : IKlineProvider
{
    private readonly IGrainFactory _factory;

    public KlineProvider(IGrainFactory factory)
    {
        _factory = factory;
    }

    public ValueTask<IReadOnlyList<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, CancellationToken cancellationToken = default)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        return _factory.GetKlineProviderReplicaGrain(symbol, interval).GetKlinesAsync();
    }

    public ValueTask<IReadOnlyList<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime tickTime, int periods, CancellationToken cancellationToken = default)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        return _factory.GetKlineProviderReplicaGrain(symbol, interval).GetKlinesAsync(tickTime, periods);
    }

    public ValueTask SetKlineAsync(Kline item, CancellationToken cancellationToken = default)
    {
        return _factory.GetKlineProviderReplicaGrain(item.Symbol, item.Interval).SetKlineAsync(item);
    }

    public ValueTask<Kline?> TryGetKlineAsync(string symbol, KlineInterval interval, DateTime openTime, CancellationToken cancellationToken = default)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        return _factory.GetKlineProviderReplicaGrain(symbol, interval).TryGetKlineAsync(openTime);
    }

    public ValueTask SetKlinesAsync(string symbol, KlineInterval interval, IEnumerable<Kline> items, CancellationToken cancellationToken = default)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));
        if (items is null) throw new ArgumentNullException(nameof(items));

        foreach (var item in items)
        {
            if (item.Symbol != symbol) throw new ArgumentOutOfRangeException(nameof(items), $"Kline has symbol '{item.Symbol}' different from partition symbol '{symbol}'");
            if (item.Interval != interval) throw new ArgumentOutOfRangeException(nameof(items), $"Kline has interval '{item.Interval}' different from partition interval '{interval}'");
        }

        return _factory.GetKlineProviderReplicaGrain(symbol, interval).SetKlinesAsync(items);
    }

    public ValueTask<DateTime?> TryGetLastOpenTimeAsync(string symbol, KlineInterval interval, CancellationToken cancellationToken = default)
    {
        return _factory.GetKlineProviderReplicaGrain(symbol, interval).TryGetLastOpenTimeAsync();
    }
}