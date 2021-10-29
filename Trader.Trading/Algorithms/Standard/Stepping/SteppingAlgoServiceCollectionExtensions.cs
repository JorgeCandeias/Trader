using Outcompute.Trader.Trading.Algorithms.Standard.Stepping;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SteppingAlgoServiceCollectionExtensions
    {
        public static IServiceCollection AddStepAlgo(this IServiceCollection trader)
        {
            return trader
                .AddAlgoType<SteppingAlgo, SteppingAlgoOptions>("Stepping");
        }
    }
}