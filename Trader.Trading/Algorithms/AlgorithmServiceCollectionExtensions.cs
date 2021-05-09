using Trader.Trading.Algorithms;
using Trader.Trading.Algorithms.Steps;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AlgorithmServiceCollectionExtensions
    {
        public static IServiceCollection AddAlgorithmResolvers(this IServiceCollection services)
        {
            return services
                .AddSingleton<ISignificantOrderResolver, SignificantOrderResolver>()
                .AddSingleton<IOrderSynchronizer, OrderSynchronizer>()
                .AddSingleton<ITradeSynchronizer, TradeSynchronizer>()
                .AddSingleton<IOrderCodeGenerator, OrderCodeGenerator>()
                .AddSingleton<ITrackingBuyStep, TrackingBuyStep>()
                .AddSingleton<IAveragingSellStep, AveragingSellStep>();
        }
    }
}