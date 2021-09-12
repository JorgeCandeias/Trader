using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class AlgoOptionsConfigurator<TOptions> : IConfigureNamedOptions<TOptions>
        where TOptions : class
    {
        private readonly AlgoConfigurationMappingOptions _mapping;
        private readonly IConfiguration _config;

        public AlgoOptionsConfigurator(IOptions<AlgoConfigurationMappingOptions> mapping, IConfiguration config)
        {
            _mapping = mapping.Value ?? throw new ArgumentNullException(nameof(mapping));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Configure(string name, TOptions options)
        {
            _config
                .GetSection(_mapping.RootKey)
                .GetSection(name)
                .GetSection(_mapping.OptionsSubKey)
                .Bind(options);
        }

        public void Configure(TOptions options)
        {
            throw new NotSupportedException();
        }
    }
}