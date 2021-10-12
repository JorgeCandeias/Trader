using Outcompute.Trader.Trading.Readyness;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ReadynessEntryServiceCollectionExtensions
    {
        public static IServiceCollection AddReadynessEntry(this IServiceCollection services, Func<ValueTask<bool>> action)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (action is null) throw new ArgumentNullException(nameof(action));

            return services.AddReadynessEntry((_, _) => action());
        }

        public static IServiceCollection AddReadynessEntry(this IServiceCollection services, Func<IServiceProvider, ValueTask<bool>> action)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (action is null) throw new ArgumentNullException(nameof(action));

            return services.AddReadynessEntry((sp, _) => action(sp));
        }

        public static IServiceCollection AddReadynessEntry(this IServiceCollection services, Func<CancellationToken, ValueTask<bool>> action)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (action is null) throw new ArgumentNullException(nameof(action));

            return services.AddReadynessEntry((_, ct) => action(ct));
        }

        public static IServiceCollection AddReadynessEntry(this IServiceCollection services, Func<IServiceProvider, CancellationToken, ValueTask<bool>> action)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (action is null) throw new ArgumentNullException(nameof(action));

            return services.AddSingleton<IReadynessEntry>(new ReadynessEntry(action));
        }
    }
}