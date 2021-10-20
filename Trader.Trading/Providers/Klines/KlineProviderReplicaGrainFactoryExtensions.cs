using Orleans.Runtime;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers.Klines;
using System;
using System.Collections.Concurrent;

namespace Orleans
{
    internal static class KlineProviderReplicaGrainFactoryExtensions
    {
        private static readonly ConcurrentDictionary<(SiloAddress Address, string Symbol, KlineInterval Interval), IKlineProviderReplicaGrain> _lookup = new();

        private static IKlineProviderReplicaGrain FactoryMethod((SiloAddress Address, string Symbol, KlineInterval Interval) key, (IGrainFactory Factory, ILocalSiloDetails Details) arg)
        {
            return arg.Factory.GetGrain<IKlineProviderReplicaGrain>($"{arg.Details.SiloAddress.ToParsableString()}|{key.Symbol}|{key.Interval}");
        }

        private static readonly Func<(SiloAddress Address, string Symbol, KlineInterval Interval), (IGrainFactory Factory, ILocalSiloDetails Details), IKlineProviderReplicaGrain> FactoryDelegate = FactoryMethod;

        public static IKlineProviderReplicaGrain GetKlineProviderReplicaGrain(this IGrainFactory factory, ILocalSiloDetails details, string symbol, KlineInterval interval)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));
            if (details is null) throw new ArgumentNullException(nameof(details));

            return _lookup.GetOrAdd((details.SiloAddress, symbol, interval), FactoryDelegate, (factory, details));
        }
    }
}