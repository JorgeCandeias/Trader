using System;
using Trader.Trading;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TradingHostServiceCollectionExtensions
    {
        public static IServiceCollection AddTradingHost(this IServiceCollection services, Action<TradingHostOptions> configure)
        {
            return services
                .AddHostedService<TradingHost>()
                .AddOptions<TradingHostOptions>()
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}