using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class AlgoManagerGrainOptionsConfigurator : IConfigureOptions<AlgoManagerGrainOptions>
    {
        private readonly AlgoConfigurationMappingOptions _mapping;
        private readonly IConfiguration _config;

        public AlgoManagerGrainOptionsConfigurator(IOptions<AlgoConfigurationMappingOptions> mapping, IConfiguration config)
        {
            _mapping = mapping.Value ?? throw new ArgumentNullException(nameof(mapping));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Configure(AlgoManagerGrainOptions options)
        {
            var section = _config.GetSection(_mapping.RootKey);

            foreach (var node in section.GetChildren())
            {
                var key = node.Key;
                var enabled = node.GetValue<bool>(_mapping.EnabledSubKey);

                options.Algos[key] = enabled;
            }
        }
    }
}