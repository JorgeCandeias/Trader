using Microsoft.Extensions.Options;
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

            return options.Algos.SelectMany(x => x.Value.DependsOn.Tickers).Distinct(StringComparer.OrdinalIgnoreCase);
        }
    }
}