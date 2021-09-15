using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Hosting;
using System;

namespace Microsoft.Extensions.Hosting
{
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

            if (!builder.Properties.ContainsKey(nameof(UseTraderCore)))
            {
                // perform one-time actions
                builder
                    .UseGrainWatchdog()
                    .UseTrader(trader =>
                    {
                        trader
                            .AddTradingServices()
                            .AddAccumulatorAlgo()
                            .AddValueAveragingAlgo()
                            .AddStepAlgo()
                            .ConfigureServices((context, services) =>
                            {
                                services
                                    // todo: make this depend on algo configuration mapping options
                                    .AddTraderAgent(options => context.Configuration.Bind("Trader:Agent", options))
                                    .AddSystemClock()
                                    .AddSafeTimerFactory()
                                    .AddBase62NumberSerializer()
                                    .AddModelAutoMapperProfiles()
                                    .AddAlgoServices()
                                    .AddRandomGenerator()
                                    .AddAlgoFactoryResolver()
                                    .AddAlgoManagerGrain()
                                    .AddAlgoHostGrain();
                            });
                    });

                builder.Properties[nameof(UseTraderCore)] = true;
            }

            return builder;
        }
    }
}