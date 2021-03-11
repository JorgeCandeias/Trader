using Microsoft.Extensions.Hosting;
using Trader.Core.Trading;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TradingHostServiceCollectionExtensions
    {
        public static IServiceCollection AddTradingHost(this IServiceCollection services)
        {
            return services
                .AddSingleton<TradingHost>()
                .AddSingleton<ITradingHost>(sp => sp.GetRequiredService<TradingHost>())
                .AddSingleton<IHostedService>(sp => sp.GetRequiredService<TradingHost>());
        }
    }
}