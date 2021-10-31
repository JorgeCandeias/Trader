using Outcompute.Trader.Trading.Algorithms.Standard.PennyAccumulator;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PennyAccumulatorServiceCollectionExtensions
    {
        public static IServiceCollection AddPennyAccumulatorAlgo(this IServiceCollection services)
        {
            return services
                .AddAlgoType<PennyAccumulatorAlgo, PennyAccumulatorOptions>("PennyAccumulator");
        }
    }
}