using Outcompute.Trader.Trading.Algorithms.Step;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class StepAlgoServiceCollectionExtensions
    {
        public static IServiceCollection AddStepAlgo(this IServiceCollection trader)
        {
            return trader
                .AddAlgoType<StepAlgo, StepAlgoOptions>("Step");
        }
    }
}