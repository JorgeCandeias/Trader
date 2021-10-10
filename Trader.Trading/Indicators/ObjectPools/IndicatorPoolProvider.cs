using Microsoft.Extensions.ObjectPool;

namespace Outcompute.Trader.Trading.Indicators.ObjectPools
{
    internal static class IndicatorPoolProvider
    {
        public static ObjectPoolProvider Default { get; } = new DefaultObjectPoolProvider();
    }
}