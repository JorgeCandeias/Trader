using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Algorithms;

public interface IAlgoDependencyResolver
{
    /// <summary>
    /// All unique trading symbols from algos that declare them.
    /// </summary>
    ISet<string> Symbols { get; }

    /// <summary>
    /// All kline intervals mapped to the max period between all algos that declare them.
    /// </summary>
    IDictionary<(string Symbol, KlineInterval Interval), int> Klines { get; }
}