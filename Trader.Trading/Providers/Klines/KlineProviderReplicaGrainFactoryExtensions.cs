using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers.Klines;
using System;

namespace Orleans
{
    internal static class KlineProviderReplicaGrainFactoryExtensions
    {
        public static IKlineProviderReplicaGrain GetKlineProviderReplicaGrain(this IGrainFactory factory, string symbol, KlineInterval interval)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            return factory.GetGrain<IKlineProviderReplicaGrain>($"{symbol}|{interval}");
        }
    }
}