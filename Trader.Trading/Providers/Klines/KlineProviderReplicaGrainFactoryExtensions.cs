using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers.Klines;
using System.Collections.Concurrent;

namespace Orleans;

internal static class KlineProviderReplicaGrainFactoryExtensions
{
    private static readonly ConcurrentDictionary<(string Symbol, KlineInterval Interval), string> _lookup = new();

    private static string FactoryMethod((string Symbol, KlineInterval Interval) key)
    {
        return $"{key.Symbol}|{key.Interval}";
    }

    private static readonly Func<(string Symbol, KlineInterval Interval), string> FactoryDelegate = FactoryMethod;

    public static IKlineProviderReplicaGrain GetKlineProviderReplicaGrain(this IGrainFactory factory, string symbol, KlineInterval interval)
    {
        if (factory is null) throw new ArgumentNullException(nameof(factory));
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        var key = _lookup.GetOrAdd((symbol, interval), FactoryDelegate);

        return factory.GetGrain<IKlineProviderReplicaGrain>(key);
    }
}