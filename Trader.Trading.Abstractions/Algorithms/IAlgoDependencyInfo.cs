using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Algorithms
{
    public interface IAlgoDependencyInfo
    {
        IEnumerable<string> GetTickers();

        IEnumerable<KlineDependency> GetKlines();
    }

    public record KlineDependency(string Symbol, KlineInterval Interval, TimeSpan Window);
}