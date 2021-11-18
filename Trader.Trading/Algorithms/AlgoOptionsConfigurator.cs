using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Trading.Configuration;

namespace Outcompute.Trader.Trading.Algorithms;

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
        if (IsNullOrEmpty(name))
        {
            throw new NotSupportedException();
        }

        _config.GetSection(_mapping.AlgosKey).GetSection(name).Bind(options);
    }

    public void Configure(AlgoOptions options)
    {
        Configure(Options.DefaultName, options);
    }
}