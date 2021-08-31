using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using Trader.Core;
using Trader.Hosting;
using static System.String;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TraderHostingExtensions
    {
        public const string TraderAlgorithmsConfigurationSectionKey = "Trader:Algos";
        public const string TraderAlgorithmsConfigurationTypeKey = "Type";
        public const string TraderAlgorithmsConfigurationOptionsKey = "Options";

        public static ITraderHostBuilder AddTraderAlgorithmsFromConfig(this ITraderHostBuilder builder)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            return builder.AddTraderAlgorithmsFromConfigSection(TraderAlgorithmsConfigurationSectionKey);
        }

        public static ITraderHostBuilder AddTraderAlgorithmsFromConfigSection(this ITraderHostBuilder builder, string section)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (IsNullOrWhiteSpace(section)) throw new ArgumentNullException(nameof(section));

            return builder.ConfigureServices((context, services) =>
            {
                foreach (var algo in context.Configuration.GetSection(section).GetChildren())
                {
                    var name = algo.Key;
                    if (IsNullOrWhiteSpace(name))
                    {
                        throw new TraderConfigurationException($"The algorithm name '{name}' must not be empty or white space.");
                    }

                    var type = algo[TraderAlgorithmsConfigurationTypeKey];
                    if (IsNullOrWhiteSpace(type))
                    {
                        throw new TraderConfigurationException($"Algorithm '{name}' does not have a valid '{TraderAlgorithmsConfigurationTypeKey}' property specified.");
                    }

                    var options = algo.GetSection(TraderAlgorithmsConfigurationOptionsKey);

                    // todo: refactor this into a registration factory pattern
                    switch (type)
                    {
                        case "Accumulator":
                            services.AddAccumulatorAlgorithm(name, options => algo.Bind("Options", options));
                            break;

                        case "ValueAveraging":
                            services.AddValueAveragingAlgorithm(name, options => algo.Bind("Options", options));
                            break;

                        case "MinimumBalance":
                            services.AddMinimumBalanceAlgorithm(name, options => algo.Bind("Options", options));
                            break;

                        case "Step":
                            services.AddStepAlgorithm(name, options => algo.Bind("Options", options));
                            break;

                        case "Change":
                            services.AddChangeAlgorithm(name, options => algo.Bind("Options", options));
                            break;

                        default:
                            throw new TraderConfigurationException($"Algorithm '{name}' has unknown type '{type}'");
                    }
                }
            });
        }

        public static ITraderHostBuilder AddTraderCore(this ITraderHostBuilder builder)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            return builder
                .ConfigureServices((context, services) =>
                {
                    services
                        .AddTradingHost(options => context.Configuration.Bind("Trader:Host"))
                        .AddSystemClock()
                        .AddSafeTimerFactory()
                        .AddBase62NumberSerializer()
                        .AddModelAutoMapperProfiles()
                        .AddTraderAlgorithmBlocks();
                })
                .AddTraderAlgorithmsFromConfig();
        }

        public static IHostBuilder UseTrader(this IHostBuilder builder, Action<ITraderHostBuilder> configure)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            return builder.UseTrader((context, trader) => configure(trader));
        }

        public static IHostBuilder UseTrader(this IHostBuilder builder, Action<HostBuilderContext, ITraderHostBuilder> configure)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            TraderHostBuilder trader;

            if (builder.Properties.ContainsKey(nameof(TraderHostBuilder)))
            {
                trader = (TraderHostBuilder)builder.Properties[nameof(TraderHostBuilder)];
            }
            else
            {
                builder.Properties[nameof(TraderHostBuilder)] = trader = new TraderHostBuilder();

                builder.ConfigureServices((context, services) => trader.Build(context, services));
            }

            trader.ConfigureTrader(configure);

            return builder;
        }
    }
}