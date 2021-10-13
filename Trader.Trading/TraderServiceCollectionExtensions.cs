using Outcompute.Trader.Trading;
using Outcompute.Trader.Trading.Readyness;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TraderServiceCollectionExtensions
    {
        public static IServiceCollection AddTradingServices(this IServiceCollection services)
        {
            return services
                .AddSingleton<IReadynessProvider, ReadynessProvider>()
                .AddOptions<SavingsOptions>().ValidateDataAnnotations().Services;
        }
    }
}