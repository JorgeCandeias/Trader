using Outcompute.Trader.Trading.Watchdog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class WatchdogEntryServiceCollectionExtensions
    {
        public static IServiceCollection AddWatchdogEntry(this IServiceCollection services, Func<IServiceProvider, CancellationToken, Task> action)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (action is null) throw new ArgumentNullException(nameof(action));

            return services.AddSingleton<IWatchdogEntry>(new WatchdogEntry(action));
        }
    }
}