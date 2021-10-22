using Outcompute.Trader.Trading.Providers.Orders;
using Outcompute.Trader.Trading.Providers.Tickers;
using System;

namespace Orleans
{
    internal static class OrderProviderReplicaGrainFactoryExtensions
    {
        public static IOrderProviderReplicaGrain GetOrderProviderReplicaGrain(this IGrainFactory factory, string symbol)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            return factory.GetGrain<IOrderProviderReplicaGrain>(symbol);
        }
    }
}