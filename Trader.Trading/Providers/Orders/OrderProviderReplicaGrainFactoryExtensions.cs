using Orleans.Runtime;
using Outcompute.Trader.Trading.Providers.Orders;
using System;
using System.Collections.Concurrent;

namespace Orleans
{
    internal static class OrderProviderReplicaGrainFactoryExtensions
    {
        private static readonly ConcurrentDictionary<(SiloAddress Address, string Symbol), string> _lookup = new();

        private static string FactoryMethod((SiloAddress Address, string Symbol) key)
        {
            return $"{key.Address.ToParsableString()}|{key.Symbol}";
        }

        private static readonly Func<(SiloAddress Address, string Symbol), string> FactoryDelegate = FactoryMethod;

        public static IOrderProviderReplicaGrain GetOrderProviderReplicaGrain(this IGrainFactory factory, SiloAddress address, string symbol)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            var key = _lookup.GetOrAdd((address, symbol), FactoryDelegate);

            return factory.GetGrain<IOrderProviderReplicaGrain>(key);
        }
    }
}