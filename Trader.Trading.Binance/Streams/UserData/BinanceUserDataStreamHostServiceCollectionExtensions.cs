using Outcompute.Trader.Trading.Binance.Streams.UserData;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class BinanceUserDataStreamHostServiceCollectionExtensions
    {
        public static IServiceCollection AddUserDataStreamHost(this IServiceCollection services, Action<BinanceUserDataStreamHostOptions> configure)
        {
            return services
                .AddHostedService<BinanceUserDataStreamHost>()
                .AddOptions<BinanceUserDataStreamHostOptions>()
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}