using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Outcompute.Trader.Trading.Watchdog;
using System;

namespace Orleans.Hosting
{
    public static class GrainWatchdogSiloBuilderExtensions
    {
        public static ISiloBuilder UseGrainWatchdog(this ISiloBuilder builder)
        {
            return builder.UseGrainWatchdog(_ => { });
        }

        public static ISiloBuilder UseGrainWatchdog(this ISiloBuilder builder, Action<GrainWatchdogOptions> configure)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            // perform one-time actions
            if (!builder.Properties.ContainsKey(nameof(UseGrainWatchdog)))
            {
                builder
                    .AddGrainExtension<IWatchdogGrainExtension, WatchdogGrainExtension>()
                    .ConfigureServices((context, services) =>
                    {
                        services
                            .AddHostedService<GrainWatchdog>()
                            .AddOptions<GrainWatchdogOptions>()
                            .ValidateDataAnnotations();
                    });

                builder.Properties[nameof(UseGrainWatchdog)] = true;
            }

            // perform cumulative actions
            builder.ConfigureServices((context, services) =>
            {
                services.Configure(configure);
            });

            return builder;
        }
    }
}