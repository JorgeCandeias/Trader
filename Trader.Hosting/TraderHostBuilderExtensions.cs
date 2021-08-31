using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Trader.Core;
using static System.String;

namespace Trader.Hosting
{
    public static class TraderHostBuilderExtensions
    {
        public static ITraderHostBuilder AddTraderAlgorithmsFromConfig(this ITraderHostBuilder builder)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            return builder.AddTraderAlgorithmsFromConfigSection($"{TraderHostBuilderConstants.TraderRootConfigurationKey}:{TraderHostBuilderConstants.TraderAlgorithmsConfigurationSectionKey}");
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

                    var type = algo[TraderHostBuilderConstants.TraderAlgorithmsConfigurationTypeKey];
                    if (IsNullOrWhiteSpace(type))
                    {
                        throw new TraderConfigurationException($"Algorithm '{name}' does not have a valid '{TraderHostBuilderConstants.TraderAlgorithmsConfigurationTypeKey}' property specified.");
                    }

                    // todo: refactor this into a registration factory pattern
                    switch (type)
                    {
                        case "Accumulator":
                            services.AddAccumulatorAlgorithm(name, options => algo.Bind(TraderHostBuilderConstants.TraderAlgorithmsConfigurationOptionsKey, options));
                            break;

                        case "ValueAveraging":
                            services.AddValueAveragingAlgorithm(name, options => algo.Bind(TraderHostBuilderConstants.TraderAlgorithmsConfigurationOptionsKey, options));
                            break;

                        case "MinimumBalance":
                            services.AddMinimumBalanceAlgorithm(name, options => algo.Bind(TraderHostBuilderConstants.TraderAlgorithmsConfigurationOptionsKey, options));
                            break;

                        case "Step":
                            services.AddStepAlgorithm(name, options => algo.Bind(TraderHostBuilderConstants.TraderAlgorithmsConfigurationOptionsKey, options));
                            break;

                        case "Change":
                            services.AddChangeAlgorithm(name, options => algo.Bind(TraderHostBuilderConstants.TraderAlgorithmsConfigurationOptionsKey, options));
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
                        .AddTraderAgent(options => context.Configuration.Bind($"{TraderHostBuilderConstants.TraderRootConfigurationKey}:{TraderHostBuilderConstants.TraderAgentSectionConfigurationKey}"))
                        .AddSystemClock()
                        .AddSafeTimerFactory()
                        .AddBase62NumberSerializer()
                        .AddModelAutoMapperProfiles()
                        .AddTraderAlgorithmBlocks();
                })
                .AddTraderAlgorithmsFromConfig();
        }
    }
}