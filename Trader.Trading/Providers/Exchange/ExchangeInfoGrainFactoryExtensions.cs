using Outcompute.Trader.Trading.Providers.Exchange;

namespace Orleans;

internal static class ExchangeInfoGrainFactoryExtensions
{
    public static IExchangeInfoGrain GetExchangeInfoGrain(this IGrainFactory factory)
    {
        if (factory is null) throw new ArgumentNullException(nameof(factory));

        return factory.GetGrain<IExchangeInfoGrain>(Guid.Empty);
    }
}