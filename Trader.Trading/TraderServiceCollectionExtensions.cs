using Outcompute.Trader.Trading;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Providers;
using Outcompute.Trader.Trading.Readyness;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TraderServiceCollectionExtensions
    {
        public static IServiceCollection AddTradingServices(this IServiceCollection services)
        {
            return services
                .AddSingleton<IReadynessProvider, ReadynessProvider>()
                .AddSingleton<ISignificantOrderResolver, SignificantOrderResolver>()
                .AddSingleton<IOrderSynchronizer, OrderSynchronizer>()
                .AddSingleton<ITradeSynchronizer, TradeSynchronizer>()
                .AddSingleton<IOrderCodeGenerator, OrderCodeGenerator>()
                .AddSingleton<IAlgoDependencyInfo, AlgoDependencyInfo>()
                .AddSingleton<ISymbolProvider, SymbolProvider>()
                .AddScoped<AlgoContext>()
                .AddScoped<IAlgoContext>(sp => sp.GetRequiredService<AlgoContext>())
                .AddOptions<AlgoConfigurationMappingOptions>().ValidateDataAnnotations().Services
                .AddOptions<SavingsOptions>().ValidateDataAnnotations().Services;
        }
    }
}