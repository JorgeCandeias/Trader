using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Data;
using Outcompute.Trader.Trading.Data.InMemory;

namespace Orleans;

public static class InMemoryTradingRepositoryClientBuilderExtensions
{
    public static IClientBuilder AddInMemoryTradingRepository(this IClientBuilder builder)
    {
        return builder
            .ConfigureServices(services =>
            {
                services.AddSingleton<ITradingRepository, InMemoryTradingRepository>();
            });
    }
}