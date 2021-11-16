using Outcompute.Trader.Trading.Providers.Balances;

namespace Orleans;

internal static class BalanceProviderReplicaGrainFactoryExtensions
{
    public static IBalanceProviderReplicaGrain GetBalanceProviderReplicaGrain(this IGrainFactory factory, string asset)
    {
        if (factory is null) throw new ArgumentNullException(nameof(factory));

        return factory.GetGrain<IBalanceProviderReplicaGrain>(asset);
    }
}