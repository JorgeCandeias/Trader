using Outcompute.Trader.Trading.Providers.Balances;

namespace Orleans;

internal static class BalanceProviderGrainFactoryExtensions
{
    public static IBalanceProviderGrain GetBalanceProviderGrain(this IGrainFactory factory, string asset)
    {
        return factory.GetGrain<IBalanceProviderGrain>(asset);
    }
}