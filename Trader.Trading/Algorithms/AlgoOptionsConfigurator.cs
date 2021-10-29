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

            _config
                .GetSection(_mapping.AlgosKey)
                .GetSection(name)
                .GetSection(_mapping.AlgoOptionsSubKey)
                .Bind(options);
        }

        public void Configure(TOptions options)
        {
            Configure(Options.DefaultName, options);
        }
    }
}