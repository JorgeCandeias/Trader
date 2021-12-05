using Outcompute.Trader.Trading.Providers.Tickers;

namespace Orleans;

internal static class TickerProviderReplicaGrainFactoryExtensions
{
    public static ITickerProviderReplicaGrain GetTickerProviderReplicaGrain(this IGrainFactory factory, string symbol)
    {
        Guard.IsNotNull(factory, nameof(factory));

        return factory.GetGrain<ITickerProviderReplicaGrain>(symbol);
    }
}