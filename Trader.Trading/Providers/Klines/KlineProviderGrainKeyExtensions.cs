using Orleans;
using Outcompute.Trader.Models;
using System;

namespace Outcompute.Trader.Trading.Providers.Klines
{
    internal static class KlineProviderGrainKeyExtensions
    {
        public static (string Symbol, KlineInterval Interval) GetPrimaryKeys(this IKlineProviderGrain grain)
        {
            if (grain is null) throw new ArgumentNullException(nameof(grain));

            var keys = grain.GetPrimaryKeyString().Split('|');
            var symbol = keys[0];
            var interval = Enum.Parse<KlineInterval>(keys[1], false);

            return (symbol, interval);
        }
    }
}