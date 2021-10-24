using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms;

namespace Outcompute.Trader.Trading.Operations.EnsureSingleOrder
{
    internal static class EnsureSingleOrderServiceCollectionExtensions
    {
        public static IServiceCollection AddEnsureSingleOrderServices(this IServiceCollection services)
        {
            return services
                .AddSingleton<IEnsureSingleOrderOperation, EnsureSingleOrderOperation>()
                .AddSingleton<IAlgoResultExecutor<EnsureSingleOrderAlgoResult>, EnsureSingleOrderExecutor>();
        }
    }
}