using Outcompute.Trader.Trading.Algorithms.Accumulator;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AccumulatorAlgoServiceCollectionExtensions
    {
        public static IServiceCollection AddAccumulatorAlgo(this IServiceCollection services)
        {
            return services
                .AddAlgoType<AccumulatorAlgo, AccumulatorAlgoOptions>("Accumulator");
        }
    }
}