using Outcompute.Trader.Trading.Providers.Exchange;

namespace Orleans;

internal static class ExchangeInfoReplicaGrainFactoryExtensions
{
    public static IExchangeInfoReplicaGrain GetExchangeInfoReplicaGrain(this IGrainFactory factory)
    {
        if (factory is null) throw new ArgumentNullException(nameof(factory));

        return factory.GetGrain<IExchangeInfoReplicaGrain>(Guid.Empty);
    }
}