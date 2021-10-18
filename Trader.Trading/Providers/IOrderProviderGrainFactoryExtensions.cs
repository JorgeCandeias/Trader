using Outcompute.Trader.Trading.Providers;

namespace Orleans
{
    internal static class IOrderProviderGrainFactoryExtensions
    {
        public static IOrderProviderGrain GetOrderProviderGrain(this IGrainFactory factory, string symbol)
        {
            return factory.GetGrain<IOrderProviderGrain>(symbol);
        }
    }
}