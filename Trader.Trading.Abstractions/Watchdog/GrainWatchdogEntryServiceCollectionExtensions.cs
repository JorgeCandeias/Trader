using Orleans;
using Outcompute.Trader.Trading.Watchdog;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class GrainWatchdogEntryServiceCollectionExtensions
    {
        public static IServiceCollection AddGrainWatchdogEntry(this IServiceCollection services, Func<IGrain> factory)
        {
            return services.AddGrainWatchdogEntry(_ => factory());
        }

        public static IServiceCollection AddGrainWatchdogEntry(this IServiceCollection services, Func<IGrainFactory, IGrain> factory)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            return services.AddSingleton<IGrainWatchdogEntry>(new GrainWatchdogEntry(factory));
        }
    }
}