using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers.Klines;
using System;
using System.Collections.Concurrent;

namespace Orleans
{
    internal static class KlineProviderReplicaGrainFactoryExtensions
    {
        private static readonly ConcurrentDictionary<(string Symbol, KlineInterval Interval), IKlineProviderReplicaGrain> _lookup = new();

        private static IKlineProviderReplicaGrain FactoryMethod((string Symbol, KlineInterval Interval) key, IGrainFactory factory)
        {
            return factory.GetGrain<IKlineProviderReplicaGrain>($"{key.Symbol}|{key.Interval}");
        }

        private static readonly Func<(string Symbol, KlineInterval Interval), IGrainFactory, IKlineProviderReplicaGrain> FactoryDelegate = FactoryMethod;

        public static IKlineProviderReplicaGrain GetKlineProviderReplicaGrain(this IGrainFactory factory, string symbol, KlineInterval interval)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            return _lookup.GetOrAdd((symbol, interval), FactoryDelegate, factory);
        }
    }
}