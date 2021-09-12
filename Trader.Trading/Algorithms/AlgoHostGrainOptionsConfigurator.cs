using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using static System.String;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class AlgoHostGrainOptionsConfigurator : IConfigureNamedOptions<AlgoHostGrainOptions>
    {
        private readonly AlgoConfigurationMappingOptions _mapping;
        private readonly IConfiguration _config;

        public AlgoHostGrainOptionsConfigurator(IOptions<AlgoConfigurationMappingOptions> mapping, IConfiguration config)
        {
            _mapping = mapping.Value ?? throw new ArgumentNullException(nameof(mapping));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Configure(string name, AlgoHostGrainOptions options)
        {
            if (IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (options is null) throw new ArgumentNullException(nameof(options));

            _config.GetSection(_mapping.RootKey).GetSection(name).Bind(options);
        }

        public void Configure(AlgoHostGrainOptions options)
        {
            throw new NotSupportedException();
        }
    }
}