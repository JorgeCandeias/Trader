using Outcompute.Trader.Trading.Providers.Tickers;
using System;

namespace Orleans
{
    internal static class TickerProviderReplicaGrainFactoryExtensions
    {
        public static ITickerProviderReplicaGrain GetTickerProviderReplicaGrain(this IGrainFactory factory, string symbol)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            return factory.GetGrain<ITickerProviderReplicaGrain>(symbol);
        }
    }
}