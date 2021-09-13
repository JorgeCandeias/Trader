using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class AlgoManagerGrainOptionsConfigurator : IConfigureOptions<AlgoManagerGrainOptions>
    {
        private readonly AlgoConfigurationMappingOptions _mapping;
        private readonly IOptionsMonitor<AlgoHostGrainOptions> _algoOptionsMonitor;
        private readonly IConfiguration _config;

        public AlgoManagerGrainOptionsConfigurator(IOptions<AlgoConfigurationMappingOptions> mapping, IOptionsMonitor<AlgoHostGrainOptions> algoOptionsMonitor, IConfiguration config)
        {
            _mapping = mapping.Value ?? throw new ArgumentNullException(nameof(mapping));
            _algoOptionsMonitor = algoOptionsMonitor ?? throw new ArgumentNullException(nameof(algoOptionsMonitor));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Configure(AlgoManagerGrainOptions options)
        {
            var section = _config.GetSection(_mapping.RootKey);

            foreach (var node in section.GetChildren())
            {
                var name = node.Key;
                var algoOptions = _algoOptionsMonitor.Get(name);

                options.Algos[name] = algoOptions;
            }
        }
    }
}