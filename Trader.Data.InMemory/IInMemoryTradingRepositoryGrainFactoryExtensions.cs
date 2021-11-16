using Outcompute.Trader.Trading.Data.InMemory;

namespace Orleans;

internal static class IInMemoryTradingRepositoryGrainFactoryExtensions
{
    public static IInMemoryTradingRepositoryGrain GetInMemoryTradingRepositoryGrain(this IGrainFactory factory)
    {
        if (factory is null) throw new ArgumentNullException(nameof(factory));

        return factory.GetGrain<IInMemoryTradingRepositoryGrain>(Guid.Empty);
    }
}