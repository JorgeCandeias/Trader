using Orleans;
using Orleans.Runtime;
using System;

namespace Outcompute.Trader.Trading.Providers.Orders
{
    internal static class OrderProviderReplicaGrainKeyExtensions
    {
        public static (SiloAddress Address, string Symbol) GetPrimaryKeys(this IOrderProviderReplicaGrain grain)
        {
            if (grain is null) throw new ArgumentNullException(nameof(grain));

            var keys = grain.GetPrimaryKeyString().Split('|');
            var silo = SiloAddress.FromParsableString(keys[0]);
            var symbol = keys[1];

            return (silo, symbol);
        }
    }
}