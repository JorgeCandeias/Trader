using Outcompute.Trader.Trading.Binance.Providers.MarketData;
using System.Collections.Concurrent;

namespace Orleans
{
    internal static class ITickerConflaterGrainFactoryExtensions
    {
        /// <summary>
        /// Caches the grain references to avoid creating garbage during key construction.
        /// </summary>
        private static readonly ConcurrentDictionary<string, ITickerConflaterGrain> _lookup = new();

        /// <summary>
        /// Creates the grain reference for the specified key.
        /// </summary>
        private static ITickerConflaterGrain FactoryMethod(string symbol, IGrainFactory factory)
        {
            return factory.GetGrain<ITickerConflaterGrain>(symbol);
        }

        /// <summary>
        /// Caches the factory method delegate to avoid creating garbage.
        /// </summary>
        private static readonly Func<string, IGrainFactory, ITickerConflaterGrain> FactoryMethodDelegate = FactoryMethod;

        /// <summary>
        /// Gets the reference for the <see cref="ITickerConflaterGrain"/> with the specified key.
        /// </summary>
        public static ITickerConflaterGrain GetTickerConflaterGrain(this IGrainFactory factory, string symbol)
        {
            Guard.IsNotNull(factory, nameof(factory));
            Guard.IsNotNull(symbol, nameof(symbol));

            return _lookup.GetOrAdd(symbol, FactoryMethodDelegate, factory);
        }
    }
}