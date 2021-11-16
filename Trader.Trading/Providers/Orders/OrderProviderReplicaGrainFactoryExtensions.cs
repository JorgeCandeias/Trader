using Outcompute.Trader.Trading.Providers.Orders;

namespace Orleans;

internal static class OrderProviderReplicaGrainFactoryExtensions
{
    public static IOrderProviderReplicaGrain GetOrderProviderReplicaGrain(this IGrainFactory factory, string symbol)
    {
        if (factory is null) throw new ArgumentNullException(nameof(factory));
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        return factory.GetGrain<IOrderProviderReplicaGrain>(symbol);
    }
}