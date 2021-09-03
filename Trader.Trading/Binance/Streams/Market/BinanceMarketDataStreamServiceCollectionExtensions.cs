using System;
using Outcompute.Trader.Trading.Binance.Streams.Market;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class BinanceMarketDataStreamServiceCollectionExtensions
    {
        public static IServiceCollection AddMarketDataStreamHost(this IServiceCollection services, Action<BinanceMarketDataStreamHostOptions> configure)
        {
            return services
                .AddHostedService<BinanceMarketDataStreamHost>()
                .AddOptions<BinanceMarketDataStreamHostOptions>()
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}