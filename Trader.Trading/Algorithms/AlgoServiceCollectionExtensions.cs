using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Steps;

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
                .AddSingleton<IRedeemSavingsStep, RedeemSavingsStep>()

                .AddTransient<IAlgoContext, AlgoContext>()

                .AddOptions<AlgoConfigurationMappingOptions>().ValidateDataAnnotations().Services;
        }
    }
}