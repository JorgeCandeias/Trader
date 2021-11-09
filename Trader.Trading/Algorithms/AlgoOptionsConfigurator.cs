using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Trading.Configuration;
using System;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class AlgoOptionsConfigurator : IConfigureNamedOptions<AlgoOptions>
    {
        private readonly AlgoConfigurationMappingOptions _mapping;
        private readonly IConfiguration _config;

        public AlgoOptionsConfigurator(IOptions<AlgoConfigurationMappingOptions> mapping, IConfiguration config)
        {
            _mapping = mapping.Value;
            _config = config;
        }

        public void Configure(string name, AlgoOptions options)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name == Options.DefaultName)
            {
                if (string.IsNullOrWhiteSpace(AlgoContext.Current.Name))
                {
                    throw new InvalidOperationException($"{nameof(AlgoContext)}.{nameof(AlgoContext.Current)}.{nameof(AlgoContext.Current.Name)} must be defined to configure default options");
                }
                else
                {
                    name = AlgoContext.Current.Name;
                }
            }

            _config.GetSection(_mapping.AlgosKey).GetSection(name).Bind(options);
        }

        public void Configure(AlgoOptions options)
        {
            Configure(Options.DefaultName, options);
        }
    }
}