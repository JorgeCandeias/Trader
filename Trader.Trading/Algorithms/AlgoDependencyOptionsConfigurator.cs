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
        foreach (var algo in algos.Algos)
        {
            // configure stuff that depends on symbols
            if (!IsNullOrEmpty(algo.Value.Symbol))
            {
                // configure symbols from default settings
                options.Symbols.Add(algo.Value.Symbol);

                // configure symbols from symbol list settings
                options.Symbols.UnionWith(algo.Value.Symbols);

                // configure tickers from default settings
                options.Tickers.Add(algo.Value.Symbol);

                // configure balances from default settings
                options.Balances.Add(algo.Value.Symbol);

                // configure klines
                if (algo.Value.KlineInterval != KlineInterval.None && algo.Value.KlinePeriods > 0)
                {
                    foreach (var symbol in options.Symbols)
                    {
                        options.Klines[(symbol, algo.Value.KlineInterval)] = algo.Value.KlinePeriods;
                    }
                }
            }

            // configure tickers from dependencies
            options.Tickers.UnionWith(algo.Value.DependsOn.Tickers);

            // configure balances from dependencies
            options.Balances.UnionWith(algo.Value.DependsOn.Balances);
        }

        // configure all symbols
        options.AllSymbols.UnionWith(options.Symbols);
        options.AllSymbols.UnionWith(options.Tickers);
        options.AllSymbols.UnionWith(options.Balances);
        options.AllSymbols.UnionWith(options.Klines.Select(x => x.Key.Symbol));
    }
}