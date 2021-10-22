using Outcompute.Trader.Trading.Providers.Trades;
using System;

namespace Orleans
{
    internal static class TradeProviderReplicaGrainFactoryExtensions
    {
        public static ITradeProviderReplicaGrain GetTradeProviderReplicaGrain(this IGrainFactory factory, string symbol)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            return factory.GetGrain<ITradeProviderReplicaGrain>(symbol);
        }
    }
}