using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Standard.Accumulator;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AccumulatorAlgoServiceCollectionExtensions
    {
        internal const string AlgoTypeName = "Accumulator";

        internal static IServiceCollection AddAccumulatorAlgoType(this IServiceCollection services)
        {
            return services
                .AddAlgoType<AccumulatorAlgo>(AlgoTypeName)
                .AddOptionsType<AccumulatorAlgoOptions>()
                .Services;
        }

        public static IAlgoBuilder AddAccumulatorAlgo(this IServiceCollection services, string name)
        {
            return services.AddAlgo(name, AlgoTypeName);
        }
    }
}