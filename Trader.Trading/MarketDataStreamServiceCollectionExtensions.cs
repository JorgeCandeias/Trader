using System;
using Trader.Trading;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MarketDataStreamServiceCollectionExtensions
    {
        public static IServiceCollection AddMarketDataStreamHost(this IServiceCollection services, Action<MarketDataStreamHostOptions> configure)
        {
            return services
                .AddHostedService<MarketDataStreamHost>()
                .AddOptions<MarketDataStreamHostOptions>()
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}