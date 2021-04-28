using System;
using Trader.Trading.Orders;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OrderBookServiceCollectionExtensions
    {
        public static IServiceCollection AddOrderBookService(this IServiceCollection services, string name, Action<OrderBookServiceOptions> configure)
        {
            return services
                .AddNamedSingleton(name, (sp, k) => ActivatorUtilities.CreateInstance<OrderBookService>(sp, k))
                .AddNamedSingleton<IOrderBookService>(name, (sp, k) => sp.GetRequiredNamedService<OrderBookService>(k))
                .AddHostedService(sp => sp.GetNamedService<OrderBookService>(name))
                .AddOptions<OrderBookServiceOptions>(name)
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}