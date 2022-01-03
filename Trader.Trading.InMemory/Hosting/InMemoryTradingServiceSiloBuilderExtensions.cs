using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading;
using Outcompute.Trader.Trading.InMemory;
using Outcompute.Trader.Trading.InMemory.UserData;

namespace Orleans.Hosting;

public static class InMemoryTradingServiceSiloBuilderExtensions
{
    public static ISiloBuilder AddInMemoryTradingService(this ISiloBuilder builder)
    {
        return builder
            .ConfigureServices(services =>
            {
                services
                    .AddSingleton<InMemoryTradingService>()
                    .AddSingleton<IInMemoryTradingService>(sp => sp.GetRequiredService<InMemoryTradingService>())
                    .AddSingleton<ITradingService>(sp => sp.GetRequiredService<InMemoryTradingService>())
                    .AddSingleton<IUserDataStreamClientFactory, InMemoryUserDataStreamClientFactory>()
                    .AddSingleton<IInMemoryUserDataStreamSender, InMemoryUserDataStreamSender>();
            })
            .ConfigureApplicationParts(manager => manager.AddApplicationPart(typeof(InMemoryTradingServiceSiloBuilderExtensions).Assembly).WithReferences());
    }
}