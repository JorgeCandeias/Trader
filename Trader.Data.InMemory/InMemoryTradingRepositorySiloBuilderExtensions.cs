using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Data;
using Outcompute.Trader.Trading.Data.InMemory;

namespace Orleans.Hosting
{
    public static class InMemoryTradingRepositorySiloBuilderExtensions
    {
        public static ISiloBuilder AddInMemoryTradingRepository(this ISiloBuilder builder)
        {
            return builder
                .ConfigureServices(services =>
                {
                    services.AddSingleton<ITradingRepository, InMemoryTradingRepository>();
                })
                .ConfigureApplicationParts(manager => manager.AddApplicationPart(typeof(InMemoryTradingRepositorySiloBuilderExtensions).Assembly).WithReferences());
        }
    }
}