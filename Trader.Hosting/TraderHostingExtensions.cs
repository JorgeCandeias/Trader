using System;
using Trader.Hosting;

namespace Microsoft.Extensions.Hosting
{
    public static class TraderHostingExtensions
    {
        public static IHostBuilder UseTrader(this IHostBuilder builder, Action<ITraderHostBuilder> configure)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            return builder.UseTrader((context, trader) => configure(trader));
        }

        public static IHostBuilder UseTrader(this IHostBuilder builder, Action<HostBuilderContext, ITraderHostBuilder> configure)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            TraderHostBuilder trader;

            if (builder.Properties.ContainsKey(nameof(TraderHostBuilder)))
            {
                trader = (TraderHostBuilder)builder.Properties[nameof(TraderHostBuilder)];
            }
            else
            {
                builder.Properties[nameof(TraderHostBuilder)] = trader = new TraderHostBuilder();

                builder.ConfigureServices((context, services) => trader.Build(context, services));
            }

            trader.ConfigureTrader(configure);

            return builder;
        }
    }
}