﻿using Microsoft.Extensions.Options;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class AlgoDependencyInfo : IAlgoDependencyInfo
    {
        private readonly IOptionsMonitor<AlgoManagerGrainOptions> _options;

        public AlgoDependencyInfo(IOptionsMonitor<AlgoManagerGrainOptions> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public IEnumerable<string> GetTickers()
        {
            var options = _options.CurrentValue;

            return options.Algos
                .SelectMany(x => x.Value.DependsOn.Tickers)
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<KlineDependency> GetKlines()
        {
            var options = _options.CurrentValue;

            return options.Algos
                .SelectMany(x => x.Value.DependsOn.Klines)
                .Select(x => new KlineDependency(x.Symbol, x.Interval, x.Periods))
                .Distinct();
        }

        public IEnumerable<KlineDependency> GetKlines(string symbol, KlineInterval interval)
        {
            var options = _options.CurrentValue;

            return options.Algos
                .SelectMany(x => x.Value.DependsOn.Klines)
                .Where(x => x.Symbol == symbol && x.Interval == interval)
                .Select(x => new KlineDependency(x.Symbol, x.Interval, x.Periods))
                .Distinct();
        }

        public IEnumerable<string> GetSymbols()
        {
            var options = _options.CurrentValue;

            var fromSymbols = options.Algos
                .Select(x => x.Value.Symbol)
                .Where(x => !string.IsNullOrEmpty(x));

            var fromBalances = options.Algos
                .SelectMany(x => x.Value.DependsOn.Balances)
                .Where(x => !string.IsNullOrEmpty(x));

            return fromSymbols.Concat(fromBalances).Distinct();
        }
    }
}