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

                // configure klines from default settings
                if (algo.Value.KlineInterval != KlineInterval.None &&
                    algo.Value.KlinePeriods > 0 &&
                    (!options.Klines.TryGetValue((algo.Value.Symbol, algo.Value.KlineInterval), out var periods) || algo.Value.KlinePeriods > periods))
                {
                    options.Klines[(algo.Value.Symbol, algo.Value.KlineInterval)] = algo.Value.KlinePeriods;
                }
            }

            // configure tickers from dependencies
            options.Tickers.UnionWith(algo.Value.DependsOn.Tickers);

            // configure balances from dependencies
            options.Balances.UnionWith(algo.Value.DependsOn.Balances);

            // configure klines from dependencies
            foreach (var dependency in algo.Value.DependsOn.Klines)
            {
                var symbol = dependency.Symbol ?? algo.Value.Symbol;
                var interval = dependency.Interval != KlineInterval.None ? dependency.Interval : algo.Value.KlineInterval;
                var periods = dependency.Periods > 0 ? dependency.Periods : algo.Value.KlinePeriods;

                if (IsNullOrEmpty(symbol))
                {
                    throw new InvalidOperationException($"Algo '{algo.Key}' declares kline dependency without '{nameof(dependency.Symbol)}' and there is no default '{nameof(algo.Value.Symbol)}' to inherit from");
                }

                if (interval == KlineInterval.None)
                {
                    throw new InvalidOperationException($"Algo '{algo.Key}' declares kline dependency without '{nameof(dependency.Interval)}' and there is no default '{nameof(algo.Value.KlineInterval)}' to inherit from");
                }

                if (periods == 0)
                {
                    throw new InvalidOperationException($"Algo '{algo.Key}' declares kline dependency without '{nameof(dependency.Periods)}' and there is no default '{nameof(algo.Value.KlinePeriods)}' to inherit from");
                }

                if (!options.Klines.TryGetValue((symbol, interval), out var value) || periods > value)
                {
                    options.Klines[(symbol, interval)] = periods;
                }
            }
        }

        // configure all symbols
        options.AllSymbols.UnionWith(options.Symbols);
        options.AllSymbols.UnionWith(options.Tickers);
        options.AllSymbols.UnionWith(options.Balances);
        options.AllSymbols.UnionWith(options.Klines.Select(x => x.Key.Symbol));
    }
}