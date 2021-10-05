using Outcompute.Trader.Trading.Algorithms;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AlgoServiceCollectionExtensions
    {
        // todo: move these to a central file
        public static IServiceCollection AddAlgoServices(this IServiceCollection services)
        {
            return services
                .AddSingleton<ISignificantOrderResolver, SignificantOrderResolver>()
                .AddSingleton<IOrderSynchronizer, OrderSynchronizer>()
                .AddSingleton<ITradeSynchronizer, TradeSynchronizer>()
                .AddSingleton<IOrderCodeGenerator, OrderCodeGenerator>()
                .AddSingleton<IAlgoDependencyInfo, AlgoDependencyInfo>()

                .AddTransient<IAlgoContext, AlgoContext>()

                .AddOptions<AlgoConfigurationMappingOptions>().ValidateDataAnnotations().Services;
        }
    }
}