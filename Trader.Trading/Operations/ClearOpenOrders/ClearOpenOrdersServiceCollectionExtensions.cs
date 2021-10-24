using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Operations;
using Outcompute.Trader.Trading.Operations.ClearOpenOrders;

namespace Microsoft.Extensions.DependencyInjection
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