using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Hosting;
using System;

namespace Microsoft.Extensions.Hosting
{
    [Obsolete("Remove")] // todo: remove this class
    public static class TraderBuilderHostBuilderExtensions
    {
        public static IHostBuilder UseTrader(this IHostBuilder builder, Action<ITraderBuilder> configure)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            return builder.UseTrader((context, trader) => configure(trader));
        }

        public static IHostBuilder UseTrader(this IHostBuilder builder, Action<HostBuilderContext, ITraderBuilder> configure)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            TraderBuilder trader;

            if (builder.Properties.ContainsKey(nameof(TraderBuilder)))
            {
                trader = (TraderBuilder)builder.Properties[nameof(TraderBuilder)];
            }
            else
            {
                builder.Properties[nameof(TraderBuilder)] = trader = new TraderBuilder();

                builder.UseTraderCore();

                builder.ConfigureServices((context, services) => trader.Build(context, services));
            }

            trader.ConfigureTrader(configure);

            return builder;
        }

        internal static IHostBuilder UseTraderCore(this IHostBuilder builder)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            if (builder.Properties.ContainsKey(nameof(UseTraderCore))) return builder;

            // perform one-time actions
            builder
                .UseOrleans(orleans =>
                {
                    //orleans.ConfigureApplicationParts(manager => manager.AddApplicationPart(typeof(TraderBuilder).Assembly).WithReferences());
                })
                .UseTrader(trader =>
                {
                    trader
                        .ConfigureServices((context, services) =>
                        {
                            services
                                .AddSystemClock()
                                .AddSafeTimerFactory()
                                .AddBase62NumberSerializer()
                                .AddModelAutoMapperProfiles()
                                .AddRandomGenerator()
                                .AddAlgoFactoryResolver()
                                .AddAlgoManagerGrain()
                                .AddAlgoHostGrain();
                        });
                });

            builder.Properties[nameof(UseTraderCore)] = true;

            return builder;
        }
    }
}