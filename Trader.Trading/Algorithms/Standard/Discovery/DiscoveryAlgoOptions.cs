using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Discovery
{
    public class DiscoveryAlgoOptions
    {
        public ISet<string> QuoteAssets { get; } = new HashSet<string>();
        public ISet<string> IgnoreSymbols { get; } = new HashSet<string>();
    }
}