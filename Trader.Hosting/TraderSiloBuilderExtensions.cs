using Microsoft.Extensions.DependencyInjection;

namespace Orleans.Hosting
{
    public static class TraderSiloBuilderExtensions
    {
        public static ISiloBuilder AddTrader(this ISiloBuilder builder)
        {
            return builder
                .ConfigureApplicationParts(manager => manager.AddApplicationPart(typeof(TraderSiloBuilderExtensions).Assembly).WithReferences())
                .ConfigureServices((context, services) =>
                {
                    services
                        .AddTradingServices()
                        .AddAccumulatorAlgo()
                        .AddValueAveragingAlgo()
                        .AddStepAlgo()
                        .AddSystemClock()
                        .AddSafeTimerFactory()
                        .AddBase62NumberSerializer()
                        .AddModelAutoMapperProfiles()
                        .AddRandomGenerator()
                        .AddAlgoFactoryResolver()
                        .AddAlgoManagerGrain()
                        .AddAlgoHostGrain();
                });
        }
    }
}