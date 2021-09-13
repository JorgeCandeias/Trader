using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Core;
using System;
using static System.String;

namespace Outcompute.Trader.Hosting
{
    // todo: remove once dynamic algo config is implemented
    public static class TraderHostBuilderExtensions
    {
        public static ITraderBuilder AddTraderAlgorithmsFromConfig(this ITraderBuilder builder)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            return builder.AddTraderAlgorithmsFromConfigSection($"{TraderHostBuilderConstants.TraderRootConfigurationKey}:{TraderHostBuilderConstants.TraderAlgorithmsConfigurationSectionKey}");
        }

        public static ITraderBuilder AddTraderAlgorithmsFromConfigSection(this ITraderBuilder builder, string section)
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

                    // todo: remove this once the algos are hosted by the algo grain host
                    switch (type)
                    {
                        case "Step":
                            services.AddStepAlgorithm(name, options => algo.Bind(TraderHostBuilderConstants.TraderAlgorithmsConfigurationOptionsKey, options));
                            break;

                        default:
                            break;
                    }
                }
            });
        }
    }
}