using Outcompute.Trader.Trading;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Providers;
using Outcompute.Trader.Trading.Providers.Exchange;
using Outcompute.Trader.Trading.Providers.Klines;
using Outcompute.Trader.Trading.Providers.Orders;
using Outcompute.Trader.Trading.Readyness;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TraderServiceCollectionExtensions
    {
        public static IServiceCollection AddTradingServices(this IServiceCollection services)
        {
            return services

                // assorted services
                .AddSingleton<IReadynessProvider, ReadynessProvider>()
                .AddSingleton<ISignificantOrderResolver, SignificantOrderResolver>()
                .AddSingleton<IOrderSynchronizer, OrderSynchronizer>()
                .AddSingleton<ITradeSynchronizer, TradeSynchronizer>()
                .AddSingleton<IOrderCodeGenerator, OrderCodeGenerator>()
                .AddSingleton<IAlgoDependencyInfo, AlgoDependencyInfo>()
                .AddOptions<AlgoConfigurationMappingOptions>().ValidateDataAnnotations().Services
                .AddOptions<SavingsOptions>().ValidateDataAnnotations().Services

                // kline provider
                .AddSingleton<IKlineProvider, KlineProvider>()
                .AddSingleton<IKlinePublisher, KlinePublisher>()

                // order provider
                .AddSingleton<IOrderProvider, OrderProvider>()
                .AddHostedService<OrderProviderExtensionsHostedService>()

                // algo context
                .AddScoped<AlgoContext>()
                .AddScoped<IAlgoContext>(sp => sp.GetRequiredService<AlgoContext>())

                // exchange info provider
                .AddSingleton<IExchangeInfoProvider, ExchangeInfoProvider>()
                .AddOptions<ExchangeInfoOptions>().ValidateDataAnnotations().Services;
        }
    }
}