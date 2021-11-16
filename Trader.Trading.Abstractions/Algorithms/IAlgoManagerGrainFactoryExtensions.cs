using Outcompute.Trader.Trading.Algorithms;

namespace Orleans;

public static class IAlgoManagerGrainFactoryExtensions
{
    public static IAlgoManagerGrain GetAlgoManagerGrain(this IGrainFactory factory)
    {
        if (factory is null) throw new ArgumentNullException(nameof(factory));

        return factory.GetGrain<IAlgoManagerGrain>(Guid.Empty);
    }
}