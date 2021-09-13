using Outcompute.Trader.Trading.Exchange;
using System;

namespace Orleans
{
    public static class IExchangeInfoReplicaGrainFactoryExtensions
    {
        public static IExchangeInfoReplicaGrain GetExchangeInfoReplicaGrain(this IGrainFactory factory)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            return factory.GetGrain<IExchangeInfoReplicaGrain>(Guid.Empty);
        }
    }
}