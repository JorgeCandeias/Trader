using Outcompute.Trader.Models;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Algorithms
{
    public interface IAlgoDependencyResolver
    {
        /// <summary>
        /// All unique trading symbols from algos that declare them.
        /// </summary>
        ISet<string> Symbols { get; }

        /// <summary>
        /// All unique ticker symbols from algos that declare them.
        /// </summary>
        ISet<string> Tickers { get; }

        /// <summary>
        /// All unique balance symbols from algos that declare them.
        /// </summary>
        ISet<string> Balances { get; }

        /// <summary>
        /// All kline intervals mapped to the max period between all algos that declare them.
        /// </summary>
        IDictionary<(string Symbol, KlineInterval Interval), int> Klines { get; }

        /// <summary>
        /// All declared symbols from all sources, including <see cref="Symbols"/>, <see cref="Tickers"/>, <see cref="Balances"/> and <see cref="Klines"/>.
        /// </summary>
        ISet<string> AllSymbols { get; }
    }
}