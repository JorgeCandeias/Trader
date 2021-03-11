using Trader.Core.Timers;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SafeTimerServiceCollectionExtensions
    {
        public static IServiceCollection AddSafeTimerFactory(this IServiceCollection services)
        {
            return services.AddSingleton<ISafeTimerFactory, SafeTimerFactory>();
        }
    }
}