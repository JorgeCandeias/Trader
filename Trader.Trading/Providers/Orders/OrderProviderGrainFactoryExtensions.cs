using Outcompute.Trader.Trading.Providers.Orders;

namespace Orleans;

internal static class OrderProviderGrainFactoryExtensions
{
    public static IOrderProviderGrain GetOrderProviderGrain(this IGrainFactory factory, string symbol)
    {
        return factory.GetGrain<IOrderProviderGrain>(symbol);
    }
}