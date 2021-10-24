using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms;

namespace Outcompute.Trader.Trading.Operations.CancelOrder
{
    internal static class CancelOrderServiceCollectionExtensions
    {
        public static IServiceCollection AddCancelOrderServices(this IServiceCollection services)
        {
            return services
                .AddSingleton<ICancelOrderOperation, CancelOrderOperation>()
                .AddSingleton<IAlgoResultExecutor<CancelOrderAlgoResult>, CancelOrderExecutor>();
        }
    }
}