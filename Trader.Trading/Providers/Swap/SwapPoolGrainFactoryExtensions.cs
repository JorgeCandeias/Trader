using Outcompute.Trader.Trading.Providers.Swap;

namespace Orleans;

internal static class SwapPoolGrainFactoryExtensions
{
    public static ISwapPoolGrain GetSwapPoolGrain(this IGrainFactory factory)
    {
        if (factory is null) throw new ArgumentNullException(nameof(factory));

        return factory.GetGrain<ISwapPoolGrain>(Guid.Empty);
    }
}