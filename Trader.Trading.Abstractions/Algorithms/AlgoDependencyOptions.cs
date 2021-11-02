using Outcompute.Trader.Models;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Algorithms
{
    /// <summary>
    /// Utility options class for gathering dependency information from various configuration keys.
    /// Automatically populated by <see cref="AlgoDependencyOptionsConfigurator"/>.
    /// </summary>
    public class AlgoDependencyOptions
    {
        public ISet<string> Tickers { get; } = new HashSet<string>();

        public IDictionary<(string Symbol, KlineInterval Interval), int> Klines { get; } = new Dictionary<(string, KlineInterval), int>();

        public ISet<string> Symbols { get; } = new HashSet<string>();
    }
}