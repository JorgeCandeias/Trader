using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Trading.Configuration;

namespace Outcompute.Trader.Trading.Providers.Swap
{
    internal class SwapPoolOptionsConfigurator : IConfigureOptions<SwapPoolOptions>
    {
        private readonly IOptionsMonitor<AlgoConfigurationMappingOptions> _mappings;
        private readonly IConfiguration _config;

        public SwapPoolOptionsConfigurator(IOptionsMonitor<AlgoConfigurationMappingOptions> mappings, IConfiguration config)
        {
            _mappings = mappings;
            _config = config;
        }

        public void Configure(SwapPoolOptions options)
        {
            _config
                .GetSection(_mappings.CurrentValue.SwapPoolKey)
                .Bind(options);
        }
    }
}