using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Algorithms
{
    public class AlgoManagerGrainOptions
    {
        public ISet<string> Names { get; } = new HashSet<string>(StringComparer.Ordinal);
    }
}