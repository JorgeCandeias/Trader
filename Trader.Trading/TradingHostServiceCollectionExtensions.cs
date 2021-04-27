using Trader.Trading;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TradingHostServiceCollectionExtensions
    {
        public static IServiceCollection AddTradingHost(this IServiceCollection services)
        {
            return services
                .AddHostedService<TradingHost>();
        }
    }
}