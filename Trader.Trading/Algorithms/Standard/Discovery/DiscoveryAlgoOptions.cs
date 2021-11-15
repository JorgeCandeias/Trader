namespace Outcompute.Trader.Trading.Algorithms.Standard.Discovery
{
    public class DiscoveryAlgoOptions
    {
        /// <summary>
        /// The assets used as quote for symbol discovery.
        /// </summary>
        public ISet<string> QuoteAssets { get; } = new HashSet<string>();

        /// <summary>
        /// Specific symbols to ignore.
        /// </summary>
        public ISet<string> IgnoreSymbols { get; } = new HashSet<string>();
    }
}