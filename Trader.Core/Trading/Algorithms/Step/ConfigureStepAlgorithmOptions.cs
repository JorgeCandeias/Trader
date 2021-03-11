using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;

namespace Trader.Core.Trading.Algorithms.Step
{
    internal class ConfigureStepAlgorithmOptions : IConfigureNamedOptions<StepAlgorithmOptions>
    {
        private readonly IConfigurationSection _configuration;

        public ConfigureStepAlgorithmOptions(IConfigurationSection configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void Configure(string name, StepAlgorithmOptions options)
        {
            _configuration.GetSection(name).Bind(options);
        }

        public void Configure(StepAlgorithmOptions options)
        {
            _configuration.Bind(options);
        }
    }
}