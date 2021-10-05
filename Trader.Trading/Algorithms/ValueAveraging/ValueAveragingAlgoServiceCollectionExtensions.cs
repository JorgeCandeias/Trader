using Outcompute.Trader.Trading.Algorithms.ValueAveraging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ValueAveragingAlgoServiceCollectionExtensions
    {
        public static IServiceCollection AddValueAveragingAlgo(this IServiceCollection services)
        {
            return services
                .AddAlgoType<ValueAveragingAlgo, ValueAveragingAlgoOptions>("ValueAveraging");
        }
    }
}