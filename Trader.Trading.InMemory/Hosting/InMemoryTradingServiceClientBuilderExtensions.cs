using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.InMemory;

namespace Orleans;

public static class InMemoryTradingServiceClientBuilderExtensions
{
    public static IClientBuilder AddInMemoryTradingService(this IClientBuilder builder)
    {
        return builder
            .ConfigureServices(services =>
            {
                services
                    .AddSingleton<IInMemoryTradingService, InMemoryTradingService>();
            });
    }
}