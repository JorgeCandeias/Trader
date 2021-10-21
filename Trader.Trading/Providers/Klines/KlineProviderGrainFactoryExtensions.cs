using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers.Klines;
using System;
using System.Collections.Concurrent;

namespace Orleans
{
    internal static class KlineProviderGrainFactoryExtensions
    {
        private static readonly ConcurrentDictionary<(string Symbol, KlineInterval Interval), IKlineProviderGrain> _lookup = new();

        private static IKlineProviderGrain FactoryMethod((string Symbol, KlineInterval Interval) key, IGrainFactory factory)
        {
            return factory.GetGrain<IKlineProviderGrain>($"{key.Symbol}|{key.Interval}");
        }

        private static readonly Func<(string Symbol, KlineInterval Interval), IGrainFactory, IKlineProviderGrain> FactoryDelegate = FactoryMethod;

        public static IKlineProviderGrain GetKlineProviderGrain(this IGrainFactory factory, string symbol, KlineInterval interval)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            return _lookup.GetOrAdd((symbol, interval), FactoryDelegate, factory);
        }
    }
}