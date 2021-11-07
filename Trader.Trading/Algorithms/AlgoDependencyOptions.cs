using Outcompute.Trader.Models;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Algorithms
{
    /// <summary>
    /// Utility options class for gathering dependency information from various configuration keys.
    /// Automatically populated by <see cref="AlgoDependencyOptionsConfigurator"/>.
    /// </summary>
    internal class AlgoDependencyOptions
    {
        public ISet<string> Tickers { get; } = new HashSet<string>();

        public IDictionary<(string Symbol, KlineInterval Interval), int> Klines { get; } = new Dictionary<(string, KlineInterval), int>();

        public ISet<string> Symbols { get; } = new HashSet<string>();

        public ISet<string> Balances { get; } = new HashSet<string>();

        public ISet<string> AllSymbols { get; } = new HashSet<string>();
    }
}