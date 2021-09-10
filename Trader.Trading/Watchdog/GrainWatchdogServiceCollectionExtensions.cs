using Outcompute.Trader.Trading.Watchdog;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class GrainWatchdogServiceCollectionExtensions
    {
        public static IServiceCollection AddGrainWatchdog(this IServiceCollection services)
        {
            return services.AddGrainWatchdog(_ => { });
        }

        public static IServiceCollection AddGrainWatchdog(this IServiceCollection services, Action<GrainWatchdogOptions> configure)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            return services
                .AddHostedService<GrainWatchdog>()
                .AddOptions<GrainWatchdogOptions>()
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}