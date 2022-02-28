using Microsoft.Extensions.Options;

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
            // combine the symbol set and the default symbol
            var symbols = algo.Value.Symbols.AsEnumerable();

            if (!IsNullOrEmpty(algo.Value.Symbol))
            {
                symbols = symbols.Append(algo.Value.Symbol);
            }

            // configure from symbol set
            foreach (var symbol in symbols)
            {
                // configure symbols
                options.Symbols.Add(symbol);

                // configure klines
                if (algo.Value.KlineInterval != KlineInterval.None
                    && algo.Value.KlinePeriods > 0
                    && (!options.Klines.TryGetValue((symbol, algo.Value.KlineInterval), out var periods) || periods < algo.Value.KlinePeriods))
                {
                    options.Klines[(symbol, algo.Value.KlineInterval)] = algo.Value.KlinePeriods;
                }
            }

            // configure from dependencies
            foreach (var dependency in algo.Value.DependsOn.Klines)
            {
                if (IsNullOrEmpty(dependency.Symbol))
                {
                    foreach (var symbol in symbols)
                    {
                        if (options.Klines.TryGetValue((symbol, dependency.Interval), out var current))
                        {
                            options.Klines[(symbol, dependency.Interval)] = Math.Max(current, dependency.Periods);
                        }
                        else
                        {
                            options.Klines[(symbol, dependency.Interval)] = dependency.Periods;
                        }
                    }
                }
                else
                {
                    if (options.Klines.TryGetValue((dependency.Symbol, dependency.Interval), out var current))
                    {
                        options.Klines[(dependency.Symbol, dependency.Interval)] = Math.Max(current, dependency.Periods);
                    }
                    else
                    {
                        options.Klines[(dependency.Symbol, dependency.Interval)] = dependency.Periods;
                    }
                }
            }
        }
    }
}