using Orleans;
using Orleans.Runtime;
using Outcompute.Trader.Models;
using System;

namespace Outcompute.Trader.Trading.Providers.Klines
{
    internal static class KlineProviderReplicaGrainKeyExtensions
    {
        public static (SiloAddress Address, string Symbol, KlineInterval Interval) GetPrimaryKeys(this IKlineProviderReplicaGrain grain)
        {
            if (grain is null) throw new ArgumentNullException(nameof(grain));

            var keys = grain.GetPrimaryKeyString().Split('|');
            var silo = SiloAddress.FromParsableString(keys[0]);
            var symbol = keys[1];
            var interval = Enum.Parse<KlineInterval>(keys[2], false);

            return (silo, symbol, interval);
        }
    }
}