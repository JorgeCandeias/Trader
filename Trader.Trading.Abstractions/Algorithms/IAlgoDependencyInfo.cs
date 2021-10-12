using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Algorithms
{
    public interface IAlgoDependencyInfo
    {
        IEnumerable<string> GetTickers();

        IEnumerable<KlineDependency> GetKlines();

        IEnumerable<KlineDependency> GetKlines(string symbol, KlineInterval interval);
    }

    public record KlineDependency(string Symbol, KlineInterval Interval, TimeSpan Window)
    {
        public static KlineDependency Empty { get; } = new KlineDependency(string.Empty, KlineInterval.None, TimeSpan.Zero);
    };
}