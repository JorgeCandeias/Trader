using Outcompute.Trader.Trading.Providers.Orders;
using System;

namespace Orleans
{
    internal static class IOrderProviderReplicaGrainExtensions
    {
        public static IOrderProviderReplicaGrain GetOrderProviderReplicaGrain(this IGrainFactory factory, string symbol)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            return factory.GetGrain<IOrderProviderReplicaGrain>(symbol);
        }
    }
}