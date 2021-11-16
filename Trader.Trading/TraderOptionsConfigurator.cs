using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Configuration;

namespace Outcompute.Trader.Trading;

internal class TraderOptionsConfigurator : IConfigureOptions<TraderOptions>
{
    private readonly AlgoConfigurationMappingOptions _mapping;
    private readonly IConfiguration _config;
    private readonly IEnumerable<IAlgoEntry> _entries;
    private readonly IOptionsMonitor<AlgoOptions> _monitor;

    public TraderOptionsConfigurator(IOptions<AlgoConfigurationMappingOptions> mapping, IConfiguration config, IEnumerable<IAlgoEntry> entries, IOptionsMonitor<AlgoOptions> monitor)
    {
        _mapping = mapping.Value;
        _config = config;
        _entries = entries;
        _monitor = monitor;
    }

    public void Configure(TraderOptions options)
    {
        // apply all settings from configuration
        _config.GetSection(_mapping.TraderKey).Bind(options);

        // apply static configuration from user code
        foreach (var entry in _entries)
        {
            options.Algos[entry.Name] = _monitor.Get(entry.Name);
        }
    }
}