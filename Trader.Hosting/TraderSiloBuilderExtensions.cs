using Microsoft.Extensions.DependencyInjection;
using Orleans.Streams;
using Outcompute.Trader.Trading;
using System;

namespace Orleans.Hosting
{
    // todo: refactor all this into add methods on the silo builder
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
                .AddSimpleMessageStreamProvider(TraderStreamOptions.DefaultStreamProviderName, options =>
                {
                    options.PubSubType = StreamPubSubType.ExplicitGrainBasedOnly;
                })
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
                        .AddRandomGenerator()
                        .AddAlgoFactoryResolver()
                        .AddAlgoManagerGrain()
                        .AddAlgoHostGrain();
                });

            builder.Properties[nameof(TryUseTraderCore)] = true;
        }
    }
}