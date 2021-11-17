using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Algorithms;

/// <summary>
/// Utility options class for gathering dependency information from various configuration keys.
/// Automatically populated by <see cref="AlgoDependencyOptionsConfigurator"/>.
/// </summary>
internal class AlgoDependencyOptions
{
    public IDictionary<(string Symbol, KlineInterval Interval), int> Klines { get; } = new Dictionary<(string, KlineInterval), int>();

    public ISet<string> Symbols { get; } = new HashSet<string>();
}