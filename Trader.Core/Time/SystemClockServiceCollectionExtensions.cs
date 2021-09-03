using Outcompute.Trader.Core.Time;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SystemClockServiceCollectionExtensions
    {
        public static IServiceCollection AddSystemClock(this IServiceCollection services)
        {
            return services.AddSingleton<ISystemClock, SystemClock>();
        }
    }
}