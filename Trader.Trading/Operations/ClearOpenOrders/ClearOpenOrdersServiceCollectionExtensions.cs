using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms;

namespace Outcompute.Trader.Trading.Operations.ClearOpenOrders
{
    internal static class ClearOpenOrdersServiceCollectionExtensions
    {
        public static IServiceCollection AddClearOpenOrdersServices(this IServiceCollection services)
        {
            return services
                .AddSingleton<IClearOpenOrdersOperation, ClearOpenOrdersOperation>()
                .AddSingleton<IAlgoResultExecutor<ClearOpenOrdersAlgoResult>, ClearOpenOrdersExecutor>();
        }
    }
}