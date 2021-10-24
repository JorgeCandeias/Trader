using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Operations;
using Outcompute.Trader.Trading.Operations.CancelOrder;

namespace Microsoft.Extensions.DependencyInjection
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