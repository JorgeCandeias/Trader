using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Algorithms
{
    public class AlgoManagerGrainOptions
    {
        public IDictionary<string, AlgoHostGrainOptions> Algos { get; } = new Dictionary<string, AlgoHostGrainOptions>();
    }
}