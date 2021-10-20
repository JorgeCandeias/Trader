using Outcompute.Trader.Models;
using System;
using System.Collections.Concurrent;

namespace Orleans.Streams
{
    internal static class KlineStreamProviderExtensions
    {
        private static readonly ConcurrentDictionary<(string Symbol, KlineInterval Interval), string> Keys = new();

        private static string FactoryMethod((string Symbol, KlineInterval Interval) key)
        {
            return $"{key.Symbol}|{key.Interval}";
        }

        private static readonly Func<(string, KlineInterval), string> FactoryDelegate = FactoryMethod;

        public static IAsyncStream<Kline> GetKlineStream(this IStreamProvider provider, string symbol, KlineInterval interval)
        {
            if (provider is null) throw new ArgumentNullException(nameof(provider));
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            var key = Keys.GetOrAdd((symbol, interval), FactoryDelegate);

            return provider.GetStream<Kline>(Guid.Empty, key);
        }
    }
}