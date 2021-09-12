using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Algorithms
{
    public class AlgoManagerGrainOptions
    {
        public IDictionary<string, bool> Algos { get; } = new Dictionary<string, bool>();
    }
}