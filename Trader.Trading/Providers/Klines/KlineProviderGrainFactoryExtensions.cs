using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers.Klines;

namespace Orleans
{
    internal static class KlineProviderGrainFactoryExtensions
    {
        public static IKlineProviderGrain GetKlineProviderGrain(this IGrainFactory factory, string symbol, KlineInterval interval)
        {
            return factory.GetGrain<IKlineProviderGrain>($"{symbol}|{interval}");
        }
    }
}