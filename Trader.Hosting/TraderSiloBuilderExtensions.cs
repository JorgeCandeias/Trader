using Microsoft.Extensions.DependencyInjection;
using System;

namespace Orleans.Hosting
{
    public static class TraderSiloBuilderExtensions
    {
        public static ISiloBuilder UseTrader(this ISiloBuilder builder, Action<ISiloBuilder> configure)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            builder.TryUseTraderCore();

            configure(builder);

            return builder;
        }

        internal static void TryUseTraderCore(this ISiloBuilder builder)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            if (builder.Properties.ContainsKey(nameof(TryUseTraderCore))) return;

            // perform one-time actions
            builder
                .ConfigureApplicationParts(manager => manager.AddApplicationPart(typeof(TraderSiloBuilderExtensions).Assembly).WithReferences())
                .UseGrainWatchdog()
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
                        .AddAlgoServices()
                        .AddRandomGenerator()
                        .AddAlgoFactoryResolver()
                        .AddAlgoManagerGrain()
                        .AddAlgoHostGrain();
                });

            builder.Properties[nameof(TryUseTraderCore)] = true;
        }
    }
}