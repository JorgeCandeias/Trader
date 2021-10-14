using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

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
            var mainSection = _config.GetSection(_mapping.TraderKey);

            options.BatchEnabled = mainSection.GetValue(nameof(options.BatchEnabled), options.BatchEnabled);
            options.BatchTickDelay = mainSection.GetValue(nameof(options.BatchTickDelay), options.BatchTickDelay);
            options.PingDelay = mainSection.GetValue(nameof(options.PingDelay), options.PingDelay);

            var algosSection = _config.GetSection(_mapping.AlgosKey);

            foreach (var key in algosSection.GetChildren().Select(x => x.Key))
            {
                var name = key;
                var algoOptions = _algoOptionsMonitor.Get(name);

                options.Algos[name] = algoOptions;
            }
        }
    }
}