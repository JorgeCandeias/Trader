using Outcompute.Trader.Trading.Watchdog;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class WatchdogServiceCollectionExtensions
    {
        public static IServiceCollection AddWatchdogService(this IServiceCollection services)
        {
            return services
                .AddHostedService<WatchdogService>()
                .AddOptions<WatchdogOptions>()
                .ValidateDataAnnotations()
                .Services;
        }
    }
}