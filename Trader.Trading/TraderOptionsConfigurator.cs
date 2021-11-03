using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Trading.Algorithms;
using System;

namespace Outcompute.Trader.Trading
{
    internal class TraderOptionsConfigurator : IConfigureOptions<TraderOptions>
    {
        private readonly AlgoConfigurationMappingOptions _mapping;
        private readonly IConfiguration _config;

        public TraderOptionsConfigurator(IOptions<AlgoConfigurationMappingOptions> mapping, IConfiguration config)
        {
            _mapping = mapping.Value ?? throw new ArgumentNullException(nameof(mapping));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Configure(TraderOptions options)
        {
            _config.GetSection(_mapping.TraderKey).Bind(options);
        }
    }
}