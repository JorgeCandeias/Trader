using Outcompute.Trader.Trading.Algorithms.Test;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TestAlgoServiceCollectionExtensions
    {
        public static IServiceCollection AddAccumulatorAlgoType(this IServiceCollection services)
        {
            return services
                .AddAlgoType<TestAlgo, TestAlgoOptions>("Test");
        }
    }
}