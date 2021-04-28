using System;
using Trader.Trading.Orders;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OrderBookServiceCollectionExtensions
    {
        public static IServiceCollection AddOrderBookService(this IServiceCollection services, string name, Action<OrderBookServiceOptions> configure)
        {
            return services
                .AddNamedSingleton<IOrderBookService, OrderBookService>(name);
        }
    }
}