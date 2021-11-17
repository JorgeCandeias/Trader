using Microsoft.Extensions.Options;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Algorithms;

internal class AlgoDependencyOptionsConfigurator : IConfigureOptions<AlgoDependencyOptions>
{
    private readonly IOptionsMonitor<TraderOptions> _monitor;

    public AlgoDependencyOptionsConfigurator(IOptionsMonitor<TraderOptions> monitor)
    {
        _monitor = monitor;
    }

    public void Configure(AlgoDependencyOptions options)
    {
        var algos = _monitor.CurrentValue;

        // configure everything in one pass
        foreach (var algo in algos.Algos.Where(x => x.Value.Enabled))
        {
            // configure symbols
            options.Symbols.UnionWith(algo.Value.Symbols);

            // configure klines
            if (algo.Value.KlineInterval != KlineInterval.None && algo.Value.KlinePeriods > 0)
            {
                foreach (var symbol in algo.Value.Symbols)
                {
                    if (!options.Klines.TryGetValue((symbol, algo.Value.KlineInterval), out var periods) || periods < algo.Value.KlinePeriods)
                    {
                        options.Klines[(symbol, algo.Value.KlineInterval)] = algo.Value.KlinePeriods;
                    }
                }
            }
        }
    }
}