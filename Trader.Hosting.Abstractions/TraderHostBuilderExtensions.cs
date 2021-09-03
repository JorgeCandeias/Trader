using Microsoft.Extensions.DependencyInjection;
using System;

namespace Outcompute.Trader.Hosting
{
    public static class TraderHostBuilderExtensions
    {
        public static ITraderHostBuilder ConfigureServices(this ITraderHostBuilder trader, Action<IServiceCollection> configure)
        {
            if (trader is null) throw new ArgumentNullException(nameof(trader));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            return trader.ConfigureServices((context, services) => configure(services));
        }

        public static ITraderHostBuilder ConfigureTrader(this ITraderHostBuilder trader, Action<ITraderHostBuilder> configure)
        {
            if (trader is null) throw new ArgumentNullException(nameof(trader));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            return trader.ConfigureTrader(configure);
        }

        public static ITraderHostBuilder Configure<TOptions>(this ITraderHostBuilder trader, Action<TOptions> configure) where TOptions : class
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