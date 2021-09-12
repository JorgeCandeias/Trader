using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Outcompute.Trader.Trading.Watchdog;
using System;

namespace Microsoft.Extensions.Hosting
{
    public static class GrainWatchdogHostBuilderExtensions
    {
        public static IHostBuilder UseGrainWatchdog(this IHostBuilder builder)
        {
            return builder.UseGrainWatchdog(_ => { });
        }

        public static IHostBuilder UseGrainWatchdog(this IHostBuilder builder, Action<GrainWatchdogOptions> configure)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            if (!builder.Properties.ContainsKey(nameof(UseGrainWatchdog)))
            {
                // perform one time actions
                builder
                    .ConfigureServices((context, services) =>
                    {
                        services
                            .AddHostedService<GrainWatchdog>()
                            .AddOptions<GrainWatchdogOptions>()
                            .Configure(configure)
                            .ValidateDataAnnotations();
                    })
                    .UseOrleans(orleans =>
                    {
                        orleans.AddGrainExtension<IWatchdogGrainExtension, WatchdogGrainExtension>();
                    });

                builder.Properties[nameof(UseGrainWatchdog)] = true;
            }
            else
            {
                // perform cumulative actions
                builder.ConfigureServices((context, services) =>
                {
                    services
                        .Configure(configure);
                });
            }

            return builder;
        }
    }
}