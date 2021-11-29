using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Binance.Providers.MarketData;
using System.Collections.Concurrent;

namespace Orleans
{
    internal static class IKlineConflaterGrainFactoryExtensions
    {
        /// <summary>
        /// Caches the grain references to avoid creating garbage during key construction.
        /// </summary>
        private static readonly ConcurrentDictionary<(string Symbol, KlineInterval Interval), IKlineConflaterGrain> _lookup = new();

        /// <summary>
        /// Creates the grain reference for the specified key.
        /// </summary>
        private static IKlineConflaterGrain FactoryMethod((string Symbol, KlineInterval Interval) key, IGrainFactory factory)
        {
            return factory.GetGrain<IKlineConflaterGrain>($"{key.Symbol}|{key.Interval}");
        }

        /// <summary>
        /// Caches the factory method delegate to avoid creating garbage.
        /// </summary>
        private static readonly Func<(string, KlineInterval), IGrainFactory, IKlineConflaterGrain> FactoryMethodDelegate = FactoryMethod;

        /// <summary>
        /// Gets the reference for the <see cref="IKlineConflaterGrain"/> with the specified key.
        /// </summary>
        public static IKlineConflaterGrain GetKlineConflaterGrain(this IGrainFactory factory, string symbol, KlineInterval interval)
        {
            Guard.IsNotNull(factory, nameof(factory));
            Guard.IsNotNull(symbol, nameof(symbol));

            return _lookup.GetOrAdd((symbol, interval), FactoryMethodDelegate, factory);
        }
    }
}