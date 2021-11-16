using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Watchdog;

namespace Orleans.Hosting;

public static class TraderSiloBuilderExtensions
{
    public static ISiloBuilder AddTradingServices(this ISiloBuilder builder)
    {
        return builder
            .AddGrainExtension<IWatchdogGrainExtension, WatchdogGrainExtension>()
            .ConfigureApplicationParts(manager => manager.AddApplicationPart(typeof(TraderSiloBuilderExtensions).Assembly).WithReferences())
            .ConfigureServices(services =>
            {
                services.AddTradingServices();
            });
    }
}