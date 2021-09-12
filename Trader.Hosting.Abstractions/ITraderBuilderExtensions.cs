using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using System;

namespace Outcompute.Trader.Hosting
{
    public static class ITraderBuilderExtensions
    {
        public static ITraderBuilder ConfigureServices(this ITraderBuilder trader, Action<IServiceCollection> configure)
        {
            if (trader is null) throw new ArgumentNullException(nameof(trader));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            return trader.ConfigureServices((context, services) => configure(services));
        }

        public static ITraderBuilder ConfigureTrader(this ITraderBuilder trader, Action<ITraderBuilder> configure)
        {
            if (trader is null) throw new ArgumentNullException(nameof(trader));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            return trader.ConfigureTrader((context, trader) => configure(trader));
        }

        public static ITraderBuilder Configure<TOptions>(this ITraderBuilder trader, Action<TOptions> configure) where TOptions : class
        {
            if (trader is null) throw new ArgumentNullException(nameof(trader));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            return trader.ConfigureServices(services =>
            {
                services.Configure(configure);
            });
        }
    }
}