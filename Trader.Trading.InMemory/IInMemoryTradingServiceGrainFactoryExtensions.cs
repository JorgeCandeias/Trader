using Outcompute.Trader.Trading.InMemory;

namespace Orleans;

internal static class IInMemoryTradingServiceGrainFactoryExtensions
{
    public static IInMemoryTradingServiceGrain GetInMemoryTradingServiceGrain(this IGrainFactory factory)
    {
        if (factory is null) throw new ArgumentNullException(nameof(factory));

        return factory.GetGrain<IInMemoryTradingServiceGrain>(Guid.Empty);
    }
}