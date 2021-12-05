using Outcompute.Trader.Trading.Providers.Trades;

namespace Orleans;

internal static class TradeProviderReplicaGrainFactoryExtensions
{
    public static ITradeProviderReplicaGrain GetTradeProviderReplicaGrain(this IGrainFactory factory, string symbol)
    {
        Guard.IsNotNull(factory, nameof(factory));

        return factory.GetGrain<ITradeProviderReplicaGrain>(symbol);
    }
}