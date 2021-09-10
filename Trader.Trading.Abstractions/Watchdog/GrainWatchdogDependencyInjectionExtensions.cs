using Orleans;
using Outcompute.Trader.Trading.Watchdog;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class GrainWatchdogDependencyInjectionExtensions
    {
        public static IServiceCollection AddWatchdogEntry(this IServiceCollection services, Func<IGrain> factory)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            return services.AddSingleton(new GrainWatchdogEntry(factory));
        }
    }
}